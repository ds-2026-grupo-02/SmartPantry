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

    [Fact]
    public void Should_Create_Producto_With_All_Details_And_Normalize_Values()
    {
        var producto = new Producto(
            Guid.NewGuid(),
            "  Crema de Avellanas  ",
            "  Hazelnut Spread  ",
            "  3017620422003  ",
            "  400 g  ",
            "  Ferrero  ",
            "  https://images.openfoodfacts.org/nutella.jpg  ",
            "  Untables,  Chocolates, untables , , Dulces  ",
            "  Azúcar, aceite de palma, avellanas  ",
            "  milk,  nuts, MILK ,  soybeans  ",
            "  e ",
            4
        );

        producto.CodigoBarras.ShouldBe("3017620422003");
        producto.Marca.ShouldBe("Ferrero");
        producto.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/nutella.jpg");
        producto.Categorias.ShouldBe("Untables, Chocolates, Dulces");
        producto.Ingredientes.ShouldBe("Azúcar, aceite de palma, avellanas");
        producto.Alergenos.ShouldBe("milk, nuts, soybeans");
        producto.NutriScore.ShouldBe("E");
        producto.Nova.ShouldBe(4);
    }

    [Fact]
    public void Should_Preserve_Nulls_When_Optional_Details_Are_Missing_Or_Whitespace()
    {
        var producto = new Producto(
            Guid.NewGuid(),
            "Agua Mineral",
            null,
            "7790000000001",
            cantidad: "   ",
            marca: "",
            imagenUrl: "   ",
            categorias: " , , ",
            ingredientes: null,
            alergenos: "  ,  ",
            nutriScore: "   ",
            nova: null
        );

        producto.Cantidad.ShouldBeNull();
        producto.Marca.ShouldBeNull();
        producto.ImagenUrl.ShouldBeNull();
        producto.Categorias.ShouldBeNull();
        producto.Ingredientes.ShouldBeNull();
        producto.Alergenos.ShouldBeNull();
        producto.NutriScore.ShouldBeNull();
        producto.Nova.ShouldBeNull();
    }

    [Theory]
    [InlineData("a", "A")]
    [InlineData("b", "B")]
    [InlineData("C", "C")]
    [InlineData("d", "D")]
    [InlineData("E", "E")]
    public void Should_Accept_Valid_NutriScore_And_Normalize_To_Uppercase(string input, string expected)
    {
        var producto = new Producto(
            Guid.NewGuid(),
            "Yogur",
            null,
            "7790000000002",
            nutriScore: input
        );

        producto.NutriScore.ShouldBe(expected);
    }

    [Theory]
    [InlineData("F")]
    [InlineData("Z")]
    [InlineData("AB")]
    [InlineData("1")]
    [InlineData("unknown")]
    public void Should_Throw_When_NutriScore_Is_Invalid(string invalidNutriScore)
    {
        Should.Throw<ArgumentException>(() =>
        {
            new Producto(
                Guid.NewGuid(),
                "Yogur",
                null,
                "7790000000002",
                nutriScore: invalidNutriScore
            );
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Should_Accept_Valid_Nova_Range(int validNova)
    {
        var producto = new Producto(
            Guid.NewGuid(),
            "Avena",
            null,
            "7790000000003",
            nova: validNova
        );

        producto.Nova.ShouldBe(validNova);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)]
    public void Should_Throw_When_Nova_Is_Out_Of_Range(int invalidNova)
    {
        Should.Throw<ArgumentException>(() =>
        {
            new Producto(
                Guid.NewGuid(),
                "Avena",
                null,
                "7790000000003",
                nova: invalidNova
            );
        });
    }

    [Fact]
    public void Should_Throw_When_Optional_Fields_Exceed_Max_Length()
    {
        var producto = new Producto(Guid.NewGuid(), "Producto Base", null, "7790000000004");

        Should.Throw<ArgumentException>(() => producto.SetMarca(new string('a', ProductoConsts.MaxMarcaLength + 1)));
        Should.Throw<ArgumentException>(() => producto.SetImagenUrl(new string('a', ProductoConsts.MaxImagenUrlLength + 1)));
        Should.Throw<ArgumentException>(() => producto.SetCategorias(new string('a', ProductoConsts.MaxCategoriasLength + 1)));
        Should.Throw<ArgumentException>(() => producto.SetIngredientes(new string('a', ProductoConsts.MaxIngredientesLength + 1)));
        Should.Throw<ArgumentException>(() => producto.SetAlergenos(new string('a', ProductoConsts.MaxAlergenosLength + 1)));
    }

    [Fact]
    public void Should_Modify_All_Details_And_Allow_Clearing_Optional_Fields_To_Null()
    {
        var producto = new Producto(
            Guid.NewGuid(),
            "Galletitas",
            "Cookies",
            "7790000000005",
            "200 g",
            "Marca A",
            "https://example.com/a.jpg",
            "Snacks",
            "Harina, azúcar",
            "gluten",
            "D",
            4
        );

        producto.ModificarDatos(
            "Galletitas Integrales",
            null,
            "7790000000005",
            "250 g",
            marca: null,
            imagenUrl: null,
            categorias: null,
            ingredientes: null,
            alergenos: null,
            nutriScore: "b",
            nova: 2
        );

        producto.NombreEs.ShouldBe("Galletitas Integrales");
        producto.NombreEn.ShouldBeNull();
        producto.Cantidad.ShouldBe("250 g");
        producto.Marca.ShouldBeNull();
        producto.ImagenUrl.ShouldBeNull();
        producto.Categorias.ShouldBeNull();
        producto.Ingredientes.ShouldBeNull();
        producto.Alergenos.ShouldBeNull();
        producto.NutriScore.ShouldBe("B");
        producto.Nova.ShouldBe(2);
    }
}
