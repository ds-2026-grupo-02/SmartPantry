using System;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace SmartPantry.Preferencias;

public class PreferenciaUsuario_Tests
{
    [Fact]
    public void Should_Normalize_And_Save_Alergenos_Ingredientes_And_Flag()
    {
        // Arrange
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var preferencia = new PreferenciaUsuario(id, usuarioId, mostrarAlertas: false);

        var alergenos = new[] { "  Gluten  ", "Soja  " };
        var ingredientes = new[] { "  Azúcar", "  Sal  " };

        // Act
        preferencia.Actualizar(alergenos, ingredientes, mostrarAlertas: true);

        // Assert
        preferencia.UsuarioId.ShouldBe(usuarioId);
        preferencia.MostrarAlertas.ShouldBeTrue();

        preferencia.Alergenos.Count.ShouldBe(2);
        preferencia.Alergenos.Select(a => a.Nombre).ShouldBe(new[] { "Gluten", "Soja" });

        preferencia.Ingredientes.Count.ShouldBe(2);
        preferencia.Ingredientes.Select(i => i.Nombre).ShouldBe(new[] { "Azúcar", "Sal" });
    }

    [Fact]
    public void Should_Reject_Duplicates_Ignoring_Case_And_Spaces()
    {
        // Arrange
        var preferencia = new PreferenciaUsuario(Guid.NewGuid(), Guid.NewGuid());
        var alergenosConDuplicado = new[] { "Gluten", "  GLUTEN  " };

        // Act & Assert
        var ex = Should.Throw<BusinessException>(() =>
        {
            preferencia.Actualizar(alergenosConDuplicado, Array.Empty<string>(), true);
        });

        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.PreferenciaDuplicado);
        ex.Data["Nombre"].ShouldBe("GLUTEN");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Reject_Empty_Or_Whitespace_Names(string? valorInvalido)
    {
        // Arrange
        var preferencia = new PreferenciaUsuario(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert para Alérgenos
        var exAlergeno = Should.Throw<BusinessException>(() =>
        {
            preferencia.Actualizar(new[] { valorInvalido! }, Array.Empty<string>(), true);
        });
        exAlergeno.Code.ShouldBe(SmartPantryDomainErrorCodes.PreferenciaNombreVacio);

        // Act & Assert para Ingredientes
        var exIngrediente = Should.Throw<BusinessException>(() =>
        {
            preferencia.Actualizar(Array.Empty<string>(), new[] { valorInvalido! }, true);
        });
        exIngrediente.Code.ShouldBe(SmartPantryDomainErrorCodes.PreferenciaNombreVacio);
    }

    [Fact]
    public void Should_Reject_Names_Exceeding_Max_Length()
    {
        // Arrange
        var preferencia = new PreferenciaUsuario(Guid.NewGuid(), Guid.NewGuid());
        var nombreLargo = new string('A', PreferenciaUsuarioConsts.MaxNombreLength + 1);

        // Act & Assert
        var ex = Should.Throw<BusinessException>(() =>
        {
            preferencia.Actualizar(new[] { nombreLargo }, Array.Empty<string>(), true);
        });

        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.PreferenciaNombreLargo);
        ex.Data["Maximo"].ShouldBe(PreferenciaUsuarioConsts.MaxNombreLength);
    }

    [Fact]
    public void Should_Reject_More_Than_Max_Items_Per_List()
    {
        // Arrange
        var preferencia = new PreferenciaUsuario(Guid.NewGuid(), Guid.NewGuid());
        var demasiadosAlergenos = Enumerable.Range(1, PreferenciaUsuarioConsts.MaxItemsPorLista + 1)
            .Select(i => $"Alergeno_{i}")
            .ToArray();

        // Act & Assert
        var ex = Should.Throw<BusinessException>(() =>
        {
            preferencia.Actualizar(demasiadosAlergenos, Array.Empty<string>(), true);
        });

        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.PreferenciaDemasiadosItems);
        ex.Data["Maximo"].ShouldBe(PreferenciaUsuarioConsts.MaxItemsPorLista);
    }

    [Fact]
    public void Should_Not_Modify_Previous_State_When_Validation_Fails()
    {
        // Arrange: estado previo válido
        var preferencia = new PreferenciaUsuario(Guid.NewGuid(), Guid.NewGuid(), mostrarAlertas: false);
        preferencia.Actualizar(new[] { "Gluten" }, new[] { "Azúcar" }, mostrarAlertas: false);

        // Act: intento de actualización donde los alérgenos son válidos pero los ingredientes fallan
        var alergenosNuevos = new[] { "Huevo", "Leche" };
        var ingredientesInvalidos = new[] { "Sal", "   " }; // El segundo está vacío

        Should.Throw<BusinessException>(() =>
        {
            preferencia.Actualizar(alergenosNuevos, ingredientesInvalidos, mostrarAlertas: true);
        });

        // Assert: El estado no cambió
        preferencia.MostrarAlertas.ShouldBeFalse();
        preferencia.Alergenos.Count.ShouldBe(1);
        preferencia.Alergenos.First().Nombre.ShouldBe("Gluten");
        preferencia.Ingredientes.Count.ShouldBe(1);
        preferencia.Ingredientes.First().Nombre.ShouldBe("Azúcar");
    }
}
