using System;
using Shouldly;
using Xunit;
using SmartPantry;

namespace SmartPantry.Productos
{
    public class Producto_Tests
    {
        [Fact]
        public void Should_Create_Valid_Producto_And_Normalize_Text()
        {
            // Act: Instanciar la entidad pasando datos con espacios extra
            var producto = new Producto(
                Guid.NewGuid(),
                "  Leche Entera  ",
                "7791234567890"
            );

            // Assert: Comprobar que los datos se guardaron y se hizo .Trim()
            producto.CodigoBarras.ShouldBe("7791234567890");
            producto.Nombre.ShouldBe("Leche Entera");
        }

        //[Theory]
        //[InlineData("")]
        //[InlineData("   ")]
        //[InlineData(null)]
        //public void Should_Throw_Exception_When_Nombre_Is_Invalid(string nombreInvalido)
        //{
        //    // Act & Assert: Verificar que el constructor rechaza textos vacíos o nulos
        //    Should.Throw<ArgumentException>(() =>
        //    {
        //        new Producto(
        //            Guid.NewGuid(),
        //            "7791234567890",
        //            nombreInvalido
        //        );
        //    });
        //}

    }
}
