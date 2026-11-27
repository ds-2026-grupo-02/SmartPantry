using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SmartPantry.CatalogoExterno;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Applications;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class OpenFoodFactsProductCatalogClientTests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IRepository<RegistroLlamadaExterna, Guid> _registroRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public OpenFoodFactsProductCatalogClientTests()
    {
        _registroRepository = GetRequiredService<IRepository<RegistroLlamadaExterna, Guid>>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        _guidGenerator = GetRequiredService<IGuidGenerator>();
        _clock = GetRequiredService<IClock>();
    }

    private OpenFoodFactsProductCatalogClient CrearClienteConHandler(
        HttpMessageHandler handler,
        IRepository<RegistroLlamadaExterna, Guid>? customRepo = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/api/v3/")
        };

        return new OpenFoodFactsProductCatalogClient(
            httpClient,
            customRepo ?? _registroRepository,
            _unitOfWorkManager,
            _guidGenerator,
            _clock,
            NullLogger<OpenFoodFactsProductCatalogClient>.Instance
        );
    }

    [Fact]
    public async Task GetByBarcodeAsync_Deberia_Registrar_Metrica_Success_Cuando_Responde_200()
    {
        var jsonValido = """
        {
          "status": "success",
          "product": {
            "code": "3017620422003",
            "product_name": "Nutella",
            "brands": "Ferrero"
          }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonValido, Encoding.UTF8, "application/json")
            });

        var client = CrearClienteConHandler(handler);

        var producto = await client.GetByBarcodeAsync("3017620422003");

        producto.ShouldNotBeNull();
        producto.Nombre.ShouldBe("Nutella");

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorCodigo);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.Success);
            registros[0].CodigoHttp.ShouldBe(200);
            registros[0].DuracionMs.ShouldBeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public async Task GetByBarcodeAsync_Deberia_Registrar_Metrica_NotFound_Cuando_Responde_404()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = CrearClienteConHandler(handler);

        var producto = await client.GetByBarcodeAsync("0000000000000");

        producto.ShouldBeNull();

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.NotFound);
            registros[0].CodigoHttp.ShouldBe(404);
        });
    }

    [Fact]
    public async Task GetByBarcodeAsync_Deberia_Registrar_Metrica_NotFound_Cuando_Responde_200_Pero_Status_No_Es_Success()
    {
        var jsonSinProducto = """
        {
          "status": "failure",
          "product": null
        }
        """;

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonSinProducto, Encoding.UTF8, "application/json")
            });

        var client = CrearClienteConHandler(handler);

        var producto = await client.GetByBarcodeAsync("9999999999999");

        producto.ShouldBeNull();

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.NotFound);
            registros[0].CodigoHttp.ShouldBe(200);
        });
    }

    [Fact]
    public async Task GetByBarcodeAsync_Deberia_Registrar_Metrica_RateLimited_Cuando_Responde_429()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        var client = CrearClienteConHandler(handler);

        var ex = await Should.ThrowAsync<CatalogoExternoException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });
        ex.TipoError.ShouldBe(TipoErrorCatalogoExterno.RateLimited);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.RateLimited);
            registros[0].CodigoHttp.ShouldBe(429);
        });
    }

    [Fact]
    public async Task GetByBarcodeAsync_Deberia_Registrar_Metrica_Error_Cuando_Responde_500()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = CrearClienteConHandler(handler);

        var ex = await Should.ThrowAsync<CatalogoExternoException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });
        ex.TipoError.ShouldBe(TipoErrorCatalogoExterno.ProveedorNoDisponible);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.Error);
            registros[0].CodigoHttp.ShouldBe(500);
        });
    }

    [Fact]
    public async Task GetByBarcodeAsync_Deberia_Registrar_Metrica_Timeout_Cuando_Lanza_TaskCanceledException()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new TaskCanceledException("Timeout simulado"));

        var client = CrearClienteConHandler(handler);

        var ex = await Should.ThrowAsync<CatalogoExternoException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });
        ex.TipoError.ShouldBe(TipoErrorCatalogoExterno.Timeout);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.Timeout);
            registros[0].CodigoHttp.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetByBarcodeAsync_Deberia_Registrar_Metrica_Error_Sin_CodigoHttp_Cuando_Lanza_HttpRequestException()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new HttpRequestException("Fallo de red simulado"));

        var client = CrearClienteConHandler(handler);

        var ex = await Should.ThrowAsync<CatalogoExternoException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });
        ex.TipoError.ShouldBe(TipoErrorCatalogoExterno.ProveedorNoDisponible);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.Error);
            registros[0].CodigoHttp.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetByBarcodeAsync_No_Deberia_Interrumpir_Respuesta_Si_Falla_Persistencia_De_Metrica()
    {
        var jsonValido = """
        {
          "status": "success",
          "product": {
            "code": "3017620422003",
            "product_name": "Nutella"
          }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonValido, Encoding.UTF8, "application/json")
            });

        var failingRepo = Substitute.For<IRepository<RegistroLlamadaExterna, Guid>>();
        failingRepo.InsertAsync(Arg.Any<RegistroLlamadaExterna>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Error simulado de base de datos"));

        var client = CrearClienteConHandler(handler, failingRepo);

        var producto = await client.GetByBarcodeAsync("3017620422003");

        producto.ShouldNotBeNull();
        producto.Nombre.ShouldBe("Nutella");
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Enviar_Post_Mapear_Hits_Y_Registrar_Metrica_Success()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var jsonRespuesta = """
        {
          "count": 25,
          "page": 2,
          "page_size": 5,
          "page_count": 5,
          "hits": [
            {
              "code": "3017620422003",
              "product_name": "Nutella",
              "product_name_es": "Nutella Crema",
              "quantity": "400 g",
              "brands": ["xx:ferrero", "xx:la-virginia"],
              "image_url": "https://images.openfoodfacts.org/nutella.jpg",
              "categories_tags": ["en:spreads", "es:cremas-de-chocolate"],
              "ingredients_text_es": "Azúcar, avellanas",
              "allergens_tags": ["en:milk", "en:nuts"],
              "nutriscore_grade": "e",
              "nova_group": "4"
            },
            {
              "code": "7790001000001",
              "product_name": "Producto Incompleto",
              "nova_group": "unknown"
            }
          ]
        }
        """;

        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedRequest = req;
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonRespuesta, Encoding.UTF8, "application/json")
            };
        });

        var client = CrearClienteConHandler(handler);

        var resultado = await client.SearchByTextAsync("nutella", pagina: 2, tamanioPagina: 5);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.Method.ShouldBe(HttpMethod.Post);
        capturedRequest.RequestUri.ShouldBe(new Uri("https://search.openfoodfacts.org/search"));

        capturedBody.ShouldNotBeNull();
        using (var doc = JsonDocument.Parse(capturedBody))
        {
            var root = doc.RootElement;
            root.GetProperty("q").GetString().ShouldBe("nutella");
            root.GetProperty("page").GetInt32().ShouldBe(2);
            root.GetProperty("page_size").GetInt32().ShouldBe(5);
            root.TryGetProperty("langs", out _).ShouldBeFalse();
            root.GetProperty("fields").EnumerateArray().Select(e => e.GetString()).ShouldContain("brands");
        }

        resultado.TotalResultados.ShouldBe(25);
        resultado.Pagina.ShouldBe(2);
        resultado.TamanioPagina.ShouldBe(5);
        resultado.TotalPaginas.ShouldBe(5);
        resultado.Items.Count.ShouldBe(2);

        var primero = resultado.Items[0];
        primero.CodigoBarras.ShouldBe("3017620422003");
        primero.Nombre.ShouldBe("Nutella");
        primero.NombreEs.ShouldBe("Nutella Crema");
        primero.Cantidad.ShouldBe("400 g");
        primero.Marca.ShouldBe("ferrero, la virginia");
        primero.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/nutella.jpg");
        primero.Categorias.ShouldBe("spreads, cremas de chocolate");
        primero.Ingredientes.ShouldBe("Azúcar, avellanas");
        primero.Alergenos.ShouldBe("milk, nuts");
        primero.NutriScore.ShouldBe("E");
        primero.Nova.ShouldBe(4);

        var segundo = resultado.Items[1];
        segundo.CodigoBarras.ShouldBe("7790001000001");
        segundo.Nombre.ShouldBe("Producto Incompleto");
        segundo.NombreEs.ShouldBeNull();
        segundo.Marca.ShouldBeNull();
        segundo.ImagenUrl.ShouldBeNull();
        segundo.Categorias.ShouldBeNull();
        segundo.Ingredientes.ShouldBeNull();
        segundo.Alergenos.ShouldBeNull();
        segundo.NutriScore.ShouldBeNull();
        segundo.Nova.ShouldBeNull();

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorTexto);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.Success);
            registros[0].CodigoHttp.ShouldBe(200);
        });
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Incluir_Langs_En_Payload_Cuando_Se_Especifican()
    {
        string? capturedBody = null;

        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"count":0,"page":1,"page_size":10,"page_count":0,"hits":[]}""", Encoding.UTF8, "application/json")
            };
        });

        var client = CrearClienteConHandler(handler);

        await client.SearchByTextAsync("yerba", pagina: 1, tamanioPagina: 10, idiomas: new[] { "ES", "en", "es" });

        capturedBody.ShouldNotBeNull();
        using var doc = JsonDocument.Parse(capturedBody);
        var langs = doc.RootElement.GetProperty("langs").EnumerateArray().Select(e => e.GetString()).ToArray();
        langs.ShouldBe(new[] { "es", "en" });
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Retornar_Vacio_Y_Registrar_NotFound_Cuando_No_Hay_Hits()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"count":0,"page":1,"page_size":20,"page_count":0,"hits":[]}""", Encoding.UTF8, "application/json")
            });

        var client = CrearClienteConHandler(handler);

        var resultado = await client.SearchByTextAsync("sinresultados123");

        resultado.Items.ShouldBeEmpty();
        resultado.TotalResultados.ShouldBe(0);
        resultado.TotalPaginas.ShouldBe(0);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorTexto);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.NotFound);
            registros[0].CodigoHttp.ShouldBe(200);
        });
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Lanzar_RateLimited_Y_Registrar_Metrica_Cuando_Responde_429()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        var client = CrearClienteConHandler(handler);

        var ex = await Should.ThrowAsync<CatalogoExternoException>(async () =>
        {
            await client.SearchByTextAsync("cafe");
        });
        ex.TipoError.ShouldBe(TipoErrorCatalogoExterno.RateLimited);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorTexto);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.RateLimited);
            registros[0].CodigoHttp.ShouldBe(429);
        });
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Lanzar_Timeout_Y_Registrar_Metrica_Cuando_Ocurre_Timeout()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new TaskCanceledException("Timeout simulado"));

        var client = CrearClienteConHandler(handler);

        var ex = await Should.ThrowAsync<CatalogoExternoException>(async () =>
        {
            await client.SearchByTextAsync("cafe");
        });
        ex.TipoError.ShouldBe(TipoErrorCatalogoExterno.Timeout);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorTexto);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.Timeout);
            registros[0].CodigoHttp.ShouldBeNull();
        });
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Lanzar_ProveedorNoDisponible_Y_Registrar_Error_Cuando_Json_Es_Invalido()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ json invalido", Encoding.UTF8, "application/json")
            });

        var client = CrearClienteConHandler(handler);

        var ex = await Should.ThrowAsync<CatalogoExternoException>(async () =>
        {
            await client.SearchByTextAsync("cafe");
        });
        ex.TipoError.ShouldBe(TipoErrorCatalogoExterno.ProveedorNoDisponible);

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.Count.ShouldBe(1);
            registros[0].TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorTexto);
            registros[0].Resultado.ShouldBe(ResultadoLlamadaExterna.Error);
            registros[0].CodigoHttp.ShouldBe(200);
        });
    }

    [Fact]
    public async Task SearchByTextAsync_Deberia_Propagar_Cancelacion_Sin_Registrar_Metrica_Cuando_Caller_Cancela()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new FakeHttpMessageHandler(_ =>
            throw new TaskCanceledException("Cancelado por el llamador", null, cts.Token));

        var client = CrearClienteConHandler(handler);

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await client.SearchByTextAsync("cafe", cancellationToken: cts.Token);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var registros = await _registroRepository.GetListAsync();
            registros.ShouldBeEmpty();
        });
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}