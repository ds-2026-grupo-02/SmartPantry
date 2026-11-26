using NSubstitute;
using Shouldly;
using SmartPantry.CatalogoExterno;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SmartPantry.Productos;

public class ProductoAppService_ExternalSearch_Tests
{
    private readonly IExternalProductCatalogClient _externalClientMock;
    private readonly IRepository<Producto, Guid> _productoRepositoryMock;
    private readonly ProductoAppService _productoAppService;

    public ProductoAppService_ExternalSearch_Tests()
    {
        _externalClientMock = Substitute.For<IExternalProductCatalogClient>();
        _productoRepositoryMock = Substitute.For<Volo.Abp.Domain.Repositories.IRepository<Producto, Guid>>();
        _productoAppService = new ProductoAppService(_productoRepositoryMock, _externalClientMock);
    }

    [Fact]
    public async Task SearchByBarcodeAsync_Deberia_Retornar_ProductoDto_Cuando_Existe_En_API_Externa()
    {
        // Arrange (Preparación)
        var barcodeValido = "3017620422003";
        var mockExternalDto = new ExternalProductDto
        {
            CodigoBarras = barcodeValido,
            Nombre = "Nutella",
            NombreEs = "Nutella",
            Cantidad = "400 g",
            Marca = "Ferrero",
            ImagenUrl = "https://images.openfoodfacts.org/nutella.jpg",
            Categorias = "Untables, Chocolates",
            Ingredientes = "Azúcar, aceite de palma, avellanas",
            Alergenos = "milk, nuts, soybeans",
            NutriScore = "E",
            Nova = 4
        };

        // Configuramos el mock para que devuelva el producto simulado al buscar este código
        _externalClientMock.GetByBarcodeAsync(barcodeValido).Returns(Task.FromResult<ExternalProductDto?>(mockExternalDto));

        var result = await _productoAppService.SearchByBarcodeAsync(new SearchProductInputDto { CodigoBarras = barcodeValido });
        result.ShouldNotBeNull();
        result.CodigoBarras.ShouldBe(barcodeValido);
        result.Nombre.ShouldBe("Nutella");
        result.NombreEs.ShouldBe("Nutella");
        result.Cantidad.ShouldBe("400 g");
        result.Marca.ShouldBe("Ferrero");
        result.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/nutella.jpg");
        result.Categorias.ShouldBe("Untables, Chocolates");
        result.Ingredientes.ShouldBe("Azúcar, aceite de palma, avellanas");
        result.Alergenos.ShouldBe("milk, nuts, soybeans");
        result.NutriScore.ShouldBe("E");
        result.Nova.ShouldBe(4);
    }

    [Fact]
    public async Task SearchByBarcodeAsync_Deberia_Lanzar_UserFriendlyException_Cuando_Producto_No_Existe()
    {
        // Arrange
        var barcodeInexistente = "0000000000000";

        // Simulamos que el proveedor externo devuelve null (404/No encontrado)
        _externalClientMock.GetByBarcodeAsync(barcodeInexistente).Returns(Task.FromResult<ExternalProductDto?>(null));

        var input = new SearchProductInputDto { CodigoBarras = barcodeInexistente };

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SearchByBarcodeAsync(input);
        });
    }

    [Fact]
    public async Task SearchByBarcodeAsync_Deberia_Lanzar_Excepcion_Si_Input_Es_Invalido_O_Vacio()
    {
        // Arrange
        var inputVacio = new SearchProductInputDto { CodigoBarras = "" };

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SearchByBarcodeAsync(inputVacio);
        });
    }

    [Fact]
    public async Task SearchByBarcodeAsync_Deberia_Preservar_Campos_Nulos_Cuando_Proveedor_No_Informa_Datos()
    {
        // Arrange (RF-09: Producto sin marca ni imagen)
        var barcode = "1234567890123";
        var mockExternalDto = new ExternalProductDto
        {
            Nombre = "Galletitas",
            Marca = null,
            ImagenUrl = null
        };

        _externalClientMock.GetByBarcodeAsync(barcode).Returns(Task.FromResult<ExternalProductDto?>(mockExternalDto));

        // Act
        var result = await _productoAppService.SearchByBarcodeAsync(new SearchProductInputDto { CodigoBarras = barcode });

        // Assert: los campos ausentes deben permanecer nulos (no inventar valores)
        result.ShouldNotBeNull();
        result.Nombre.ShouldBe("Galletitas");
        result.NombreEs.ShouldBeNull();
        result.Cantidad.ShouldBeNull();
        result.Marca.ShouldBeNull();
        result.ImagenUrl.ShouldBeNull();
        result.Categorias.ShouldBeNull();
        result.Ingredientes.ShouldBeNull();
        result.Alergenos.ShouldBeNull();
        result.NutriScore.ShouldBeNull();
        result.Nova.ShouldBeNull();
    }

    [Fact]
    public async Task SaveFromExternalAsync_Deberia_Lanzar_UserFriendlyException_Si_Input_Es_Vacio()
    {
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SaveFromExternalAsync(new SearchProductInputDto { CodigoBarras = "   " });
        });
    }

    [Fact]
    public async Task SaveFromExternalAsync_Deberia_Lanzar_UserFriendlyException_Cuando_Producto_No_Existe_En_API_Externa()
    {
        var barcode = "0000000000001";
        _productoRepositoryMock
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Producto, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Producto?>(null));
        _externalClientMock.GetByBarcodeAsync(barcode).Returns(Task.FromResult<ExternalProductDto?>(null));

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SaveFromExternalAsync(new SearchProductInputDto { CodigoBarras = barcode });
        });
    }

    [Fact]
    public async Task SaveFromExternalAsync_Deberia_Lanzar_UserFriendlyException_Cuando_Producto_Externo_No_Tiene_Nombre()
    {
        var barcode = "0000000000002";
        _productoRepositoryMock
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Producto, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Producto?>(null));
        _externalClientMock.GetByBarcodeAsync(barcode).Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
        {
            CodigoBarras = barcode,
            Nombre = null,
            NombreEs = null,
            Marca = "Marca sin nombre"
        }));

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SaveFromExternalAsync(new SearchProductInputDto { CodigoBarras = barcode });
        });
    }
}