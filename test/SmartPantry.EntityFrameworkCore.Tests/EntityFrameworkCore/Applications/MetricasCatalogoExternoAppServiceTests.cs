using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.CatalogoExterno;
using SmartPantry.Metricas;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Applications;

public class MetricasCatalogoExternoAppServiceTests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IMetricasCatalogoExternoAppService _metricasAppService;
    private readonly IRepository<RegistroLlamadaExterna, Guid> _registroRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public MetricasCatalogoExternoAppServiceTests()
    {
        _metricasAppService = GetRequiredService<IMetricasCatalogoExternoAppService>();
        _registroRepository = GetRequiredService<IRepository<RegistroLlamadaExterna, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public void MetricasCatalogoExternoAppService_Deberia_Exigir_Permiso_De_Catalogo_Externo()
    {
        var authorizeAttribute = typeof(MetricasCatalogoExternoAppService)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .ShouldHaveSingleItem() as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

        authorizeAttribute.ShouldNotBeNull();
        authorizeAttribute.Policy.ShouldBe(Permissions.SmartPantryPermissions.Metricas.VerCatalogoExterno);
    }

    [Fact]
    public async Task GetResumenAsync_Deberia_Calcular_Agregaciones_Latencias_Y_Serie_Diaria_Correctamente()
    {
        // Arrange: cargamos registros de distintos resultados y dos días diferentes
        var dia1 = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);
        var dia2 = new DateTime(2026, 11, 26, 14, 0, 0, DateTimeKind.Utc);

        await WithUnitOfWorkAsync(async () =>
        {
            await _registroRepository.InsertManyAsync(new List<RegistroLlamadaExterna>
            {
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia1, 200, ResultadoLlamadaExterna.Success, 200),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia1.AddHours(1), 300, ResultadoLlamadaExterna.NotFound, 404),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia2, 500, ResultadoLlamadaExterna.Success, 200),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia2.AddMinutes(10), 1000, ResultadoLlamadaExterna.RateLimited, 429),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia2.AddMinutes(20), 2000, ResultadoLlamadaExterna.Timeout, null)
            }, autoSave: true);
        });

        // Act
        var resumen = await _metricasAppService.GetResumenAsync(new GetMetricasCatalogoExternoInput
        {
            FechaDesdeUtc = dia1.AddHours(-1),
            FechaHastaUtc = dia2.AddHours(1)
        });

        // Assert
        resumen.TotalLlamadas.ShouldBe(5);
        resumen.LlamadasExitosas.ShouldBe(2);
        resumen.LlamadasNoEncontradas.ShouldBe(1);
        resumen.LlamadasRateLimited.ShouldBe(1);
        resumen.LlamadasTimeout.ShouldBe(1);
        resumen.LlamadasError.ShouldBe(0);

        // 2 exitosas de 5 = 40% | 2 fallidas reales (429 + Timeout) de 5 = 40% (NotFound no suma como error)
        resumen.PorcentajeExito.ShouldBe(40.0);
        resumen.PorcentajeError.ShouldBe(40.0);

        // Latencias: [200, 300, 500, 1000, 2000] -> Promedio = 800, P95 = 2000, Max = 2000
        resumen.LatenciaPromedioMs.ShouldBe(800.0);
        resumen.LatenciaP95Ms.ShouldBe(2000);
        resumen.LatenciaMaximaMs.ShouldBe(2000);

        // Serie diaria agrupada en 2 días
        resumen.SerieDiaria.Count.ShouldBe(2);
        resumen.SerieDiaria[0].FechaUtc.Date.ShouldBe(dia1.Date);
        resumen.SerieDiaria[0].TotalLlamadas.ShouldBe(2);
        resumen.SerieDiaria[0].Exitosas.ShouldBe(1);
        resumen.SerieDiaria[0].Fallidas.ShouldBe(0);

        resumen.SerieDiaria[1].FechaUtc.Date.ShouldBe(dia2.Date);
        resumen.SerieDiaria[1].TotalLlamadas.ShouldBe(3);
        resumen.SerieDiaria[1].Exitosas.ShouldBe(1);
        resumen.SerieDiaria[1].Fallidas.ShouldBe(2);
    }

    [Fact]
    public async Task GetResumenAsync_Deberia_Lanzar_UserFriendlyException_Si_Rango_De_Fechas_Es_Invalido()
    {
        var inputInvalido = new GetMetricasCatalogoExternoInput
        {
            FechaDesdeUtc = new DateTime(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc),
            FechaHastaUtc = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc)
        };

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await _metricasAppService.GetResumenAsync(inputInvalido);
        });
    }
}