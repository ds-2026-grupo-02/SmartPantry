using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SmartPantry.CatalogoExterno;
using Volo.Abp;
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

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });

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

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });

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

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });

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

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await client.GetByBarcodeAsync("3017620422003");
        });

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