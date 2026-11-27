using NSubstitute;
using Shouldly;
using SmartPantry.CatalogoExterno;
using System;
using System.Collections.Generic;
using System.Linq;
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

    [Fact]
    public async Task SearchByTextAsync_Deberia_Retornar_Resultados_Paginados_Y_Normalizar_Parametros()
    {
        var esperado = new ResultadoBusquedaCatalogoExternoDto
        {
            Items = new[]
            {
                new ExternalProductDto
                {
                    CodigoBarras = "3017620422003",
                    Nombre = "Nutella",
                    NombreEs = "Nutella",
                    Marca = "ferrero",
                    Categorias = "untables, chocolates",
                    NutriScore = "E",
                    Nova = 4
                }
            },
            TotalResultados = 42,
            Pagina = 2,
            TamanioPagina = 10,
            TotalPaginas = 5
        };

        _externalClientMock
            .SearchByTextAsync(
                "nutella",
                2,
                10,
                Arg.Is<IReadOnlyList<string>?>(langs => langs != null && langs.SequenceEqual(new[] { "es", "en" })),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(esperado));

        var result = await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto
        {
            Texto = "  nutella  ",
            Pagina = 2,
            TamanioPagina = 10,
            Idiomas = new List<string> { " ES ", "en", "es", "   " }
        });

        result.ShouldNotBeNull();
        result.TotalResultados.ShouldBe(42);
        result.Pagina.ShouldBe(2);
        result.TamanioPagina.ShouldBe(10);
        result.TotalPaginas.ShouldBe(5);
        result.Items.Count.ShouldBe(1);
        result.Items[0].CodigoBarras.ShouldBe("3017620422003");
        result.Items[0].Marca.ShouldBe("ferrero");
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Enviar_Idiomas_Nulos_Cuando_No_Se_Especifican()
    {
        var esperado = new ResultadoBusquedaCatalogoExternoDto
        {
            Items = Array.Empty<ExternalProductDto>(),
            TotalResultados = 0,
            Pagina = 1,
            TamanioPagina = 20,
            TotalPaginas = 0
        };

        _externalClientMock
            .SearchByTextAsync("cafe", 1, 20, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(esperado));

        var result = await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto
        {
            Texto = "cafe",
            Idiomas = new List<string> { "   ", "" }
        });

        result.Items.ShouldBeEmpty();
        await _externalClientMock.Received(1)
            .SearchByTextAsync("cafe", 1, 20, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Retornar_Lista_Vacia_Sin_Excepcion_Cuando_No_Hay_Resultados()
    {
        _externalClientMock
            .SearchByTextAsync("inexistente", 1, 20, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ResultadoBusquedaCatalogoExternoDto
            {
                Items = Array.Empty<ExternalProductDto>(),
                TotalResultados = 0,
                Pagina = 1,
                TamanioPagina = 20,
                TotalPaginas = 0
            }));

        var result = await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto
        {
            Texto = "inexistente"
        });

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalResultados.ShouldBe(0);
        result.TotalPaginas.ShouldBe(0);
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Retornar_Resultado_Vacio_Cuando_Cliente_Devuelve_Null()
    {
        _externalClientMock
            .SearchByTextAsync("inexistente", 3, 15, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoBusquedaCatalogoExternoDto>(null!));

        var result = await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto
        {
            Texto = "inexistente",
            Pagina = 3,
            TamanioPagina = 15
        });

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalResultados.ShouldBe(0);
        result.Pagina.ShouldBe(3);
        result.TamanioPagina.ShouldBe(15);
        result.TotalPaginas.ShouldBe(0);
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Preservar_Campos_Nulos_En_Productos_Incompletos()
    {
        _externalClientMock
            .SearchByTextAsync("agua", 1, 10, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ResultadoBusquedaCatalogoExternoDto
            {
                Items = new[]
                {
                    new ExternalProductDto
                    {
                        CodigoBarras = "7790001000001",
                        Nombre = "Agua Mineral",
                        NombreEs = null,
                        Cantidad = null,
                        Marca = null,
                        ImagenUrl = null,
                        Categorias = null,
                        Ingredientes = null,
                        Alergenos = null,
                        NutriScore = null,
                        Nova = null
                    }
                },
                TotalResultados = 1,
                Pagina = 1,
                TamanioPagina = 10,
                TotalPaginas = 1
            }));

        var result = await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto
        {
            Texto = "agua",
            Pagina = 1,
            TamanioPagina = 10
        });

        result.Items.Count.ShouldBe(1);
        var item = result.Items[0];
        item.Nombre.ShouldBe("Agua Mineral");
        item.NombreEs.ShouldBeNull();
        item.Cantidad.ShouldBeNull();
        item.Marca.ShouldBeNull();
        item.ImagenUrl.ShouldBeNull();
        item.Categorias.ShouldBeNull();
        item.Ingredientes.ShouldBeNull();
        item.Alergenos.ShouldBeNull();
        item.NutriScore.ShouldBeNull();
        item.Nova.ShouldBeNull();
    }

    [Theory]
    [InlineData("", 1, 20)]
    [InlineData("   ", 1, 20)]
    [InlineData("cafe", 0, 20)]
    [InlineData("cafe", 1, 0)]
    [InlineData("cafe", 1, 51)]
    [InlineData("cafe", 500, 50)]
    public async Task SearchByTextAsync_Deberia_Lanzar_UserFriendlyException_Ante_Entradas_Invalidas(
        string texto,
        int pagina,
        int tamanioPagina)
    {
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto
            {
                Texto = texto,
                Pagina = pagina,
                TamanioPagina = tamanioPagina
            });
        });

        await _externalClientMock.DidNotReceiveWithAnyArgs()
            .SearchByTextAsync(default!, default, default, default, default);
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Lanzar_UserFriendlyException_Cuando_Texto_Supera_200_Caracteres()
    {
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto
            {
                Texto = new string('a', 201)
            });
        });
    }

    [Theory]
    [InlineData(TipoErrorCatalogoExterno.RateLimited, SmartPantryDomainErrorCodes.CatalogoExternoRateLimit)]
    [InlineData(TipoErrorCatalogoExterno.Timeout, SmartPantryDomainErrorCodes.CatalogoExternoTimeout)]
    [InlineData(TipoErrorCatalogoExterno.ProveedorNoDisponible, SmartPantryDomainErrorCodes.CatalogoExternoNoDisponible)]
    public async Task SearchByTextAsync_Deberia_Traducir_Fallas_Externas_A_UserFriendlyException_Con_Codigo(
        TipoErrorCatalogoExterno tipoError,
        string codigoEsperado)
    {
        _externalClientMock
            .SearchByTextAsync("cafe", 1, 20, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResultadoBusquedaCatalogoExternoDto>(
                new CatalogoExternoException(tipoError, "Error simulado")));

        var ex = await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SearchByTextAsync(new BuscarProductoPorTextoInputDto { Texto = "cafe" });
        });

        ex.Code.ShouldBe(codigoEsperado);
    }

    [Theory]
    [InlineData(TipoErrorCatalogoExterno.RateLimited, SmartPantryDomainErrorCodes.CatalogoExternoRateLimit)]
    [InlineData(TipoErrorCatalogoExterno.Timeout, SmartPantryDomainErrorCodes.CatalogoExternoTimeout)]
    [InlineData(TipoErrorCatalogoExterno.ProveedorNoDisponible, SmartPantryDomainErrorCodes.CatalogoExternoNoDisponible)]
    public async Task SearchByBarcodeAsync_Deberia_Traducir_Fallas_Externas_A_UserFriendlyException_Con_Codigo(
        TipoErrorCatalogoExterno tipoError,
        string codigoEsperado)
    {
        _externalClientMock
            .GetByBarcodeAsync("3017620422003", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExternalProductDto?>(
                new CatalogoExternoException(tipoError, "Error simulado")));

        var ex = await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SearchByBarcodeAsync(new SearchProductInputDto { CodigoBarras = "3017620422003" });
        });

        ex.Code.ShouldBe(codigoEsperado);
    }

    [Theory]
    [InlineData(TipoErrorCatalogoExterno.RateLimited, SmartPantryDomainErrorCodes.CatalogoExternoRateLimit)]
    [InlineData(TipoErrorCatalogoExterno.Timeout, SmartPantryDomainErrorCodes.CatalogoExternoTimeout)]
    [InlineData(TipoErrorCatalogoExterno.ProveedorNoDisponible, SmartPantryDomainErrorCodes.CatalogoExternoNoDisponible)]
    public async Task SaveFromExternalAsync_Deberia_Traducir_Fallas_Externas_A_UserFriendlyException_Con_Codigo(
        TipoErrorCatalogoExterno tipoError,
        string codigoEsperado)
    {
        var barcode = "3017620422003";
        _productoRepositoryMock
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Producto, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Producto?>(null));
        _externalClientMock
            .GetByBarcodeAsync(barcode, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExternalProductDto?>(
                new CatalogoExternoException(tipoError, "Error simulado")));

        var ex = await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _productoAppService.SaveFromExternalAsync(new SearchProductInputDto { CodigoBarras = barcode });
        });

        ex.Code.ShouldBe(codigoEsperado);
    }
}