using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Productos;

public class Producto_Tests
{
    [Fact]
    public void Should_Create_Valid_Producto_And_Normalize_Text()
    {
        // Act: Instanciar la entidad pasando datos con espacios extra
        var producto = new Producto(
            Guid.NewGuid(),
            "  Leche Entera  ",
            "  Whole Milk  ",
            "7791234567890",
            "  1 L  "
        );

        // Assert: Comprobar que los datos se guardaron y se hizo .Trim()
        producto.CodigoBarras.ShouldBe("7791234567890");
        producto.NombreEs.ShouldBe("Leche Entera");
        producto.NombreEn.ShouldBe("Whole Milk");
        producto.Cantidad.ShouldBe("1 L");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, null)]
    public void Should_Throw_Exception_When_Both_Nombres_Are_Empty(string? nombreEs, string? nombreEn)
    {
        Should.Throw<ArgumentException>(() =>
        {
            new Producto(
                Guid.NewGuid(),
                nombreEs,
                nombreEn,
                "7791234567890"
            );
        });
    }
}
