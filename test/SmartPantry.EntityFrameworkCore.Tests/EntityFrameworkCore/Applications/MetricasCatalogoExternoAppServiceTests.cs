using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SmartPantry.CatalogoExterno;
using SmartPantry.Metricas;
using SmartPantry.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Volo.Abp.SimpleStateChecking;
using Xunit;
using Volo.Abp.MultiTenancy;

namespace SmartPantry.EntityFrameworkCore.Applications;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class MetricasCatalogoExternoAppServiceTests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IMetricasCatalogoExternoAppService _metricasAppService;
    private readonly IRepository<RegistroLlamadaExterna, Guid> _registroRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly ICurrentTenant _currentTenant;
    private readonly IPermissionManager _permissionManager;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionValueProviderManager _permissionValueProviderManager;
    private readonly ISimpleStateCheckerManager<PermissionDefinition> _stateCheckerManager;
    private readonly IAbpAuthorizationPolicyProvider _policyProvider;
    private readonly IAuthorizationHandlerContextFactory _handlerContextFactory;
    private readonly IAuthorizationEvaluator _authorizationEvaluator;
    private readonly IOptions<AuthorizationOptions> _authorizationOptions;

    public MetricasCatalogoExternoAppServiceTests()
    {
        _metricasAppService = GetRequiredService<IMetricasCatalogoExternoAppService>();
        _registroRepository = GetRequiredService<IRepository<RegistroLlamadaExterna, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
        _permissionManager = GetRequiredService<IPermissionManager>();
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
        _permissionValueProviderManager = GetRequiredService<IPermissionValueProviderManager>();
        _stateCheckerManager = GetRequiredService<ISimpleStateCheckerManager<PermissionDefinition>>();
        _policyProvider = GetRequiredService<IAbpAuthorizationPolicyProvider>();
        _handlerContextFactory = GetRequiredService<IAuthorizationHandlerContextFactory>();
        _authorizationEvaluator = GetRequiredService<IAuthorizationEvaluator>();
        _authorizationOptions = GetRequiredService<IOptions<AuthorizationOptions>>();
    }

    private (IAbpAuthorizationService AuthService, IPermissionChecker PermissionChecker) CrearAutorizacionReal()
    {
        var realPermissionChecker = new PermissionChecker(
            _currentPrincipalAccessor,
            _permissionDefinitionManager,
            _currentTenant,
            _permissionValueProviderManager,
            _stateCheckerManager
        );

        var realHandlerProvider = new DefaultAuthorizationHandlerProvider(
            new IAuthorizationHandler[] { new PermissionRequirementHandler(realPermissionChecker) }
        );

        var realAuthService = new AbpAuthorizationService(
            _policyProvider,
            realHandlerProvider,
            NullLogger<DefaultAuthorizationService>.Instance,
            _handlerContextFactory,
            _authorizationEvaluator,
            _authorizationOptions,
            _currentPrincipalAccessor,
            ServiceProvider
        );

        return (realAuthService, realPermissionChecker);
    }

    [Fact]
    public async Task Autorizacion_Real_Deberia_Denegar_Anonimo_Y_Usuario_Sin_Permiso_Y_Permitir_Con_Permiso()
    {
        var authorizeAttribute = typeof(MetricasCatalogoExternoAppService)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .ShouldHaveSingleItem() as AuthorizeAttribute;

        authorizeAttribute.ShouldNotBeNull();
        authorizeAttribute.Policy.ShouldBe(SmartPantryPermissions.Metricas.VerCatalogoExterno);

        var (realAuthService, realPermissionChecker) = CrearAutorizacionReal();

        // 1. Usuario anónimo -> debe lanzar AbpAuthorizationException
        using (_currentPrincipalAccessor.Change(new ClaimsPrincipal(new ClaimsIdentity())))
        {
            (await realPermissionChecker.IsGrantedAsync(SmartPantryPermissions.Metricas.VerCatalogoExterno))
                .ShouldBeFalse();

            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await realAuthService.CheckAsync(SmartPantryPermissions.Metricas.VerCatalogoExterno);
            });
        }

        // 2. Usuario autenticado sin permiso otorgado -> debe lanzar AbpAuthorizationException
        var userIdSinPermiso = Guid.NewGuid();
        var principalSinPermiso = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, userIdSinPermiso.ToString()),
            new Claim(AbpClaimTypes.UserName, "usuario.sin.permiso")
        }, "TestAuth"));

        using (_currentPrincipalAccessor.Change(principalSinPermiso))
        {
            (await realPermissionChecker.IsGrantedAsync(SmartPantryPermissions.Metricas.VerCatalogoExterno))
                .ShouldBeFalse();

            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await realAuthService.CheckAsync(SmartPantryPermissions.Metricas.VerCatalogoExterno);
            });
        }

        // 3. Usuario autenticado con permiso otorgado -> debe autorizar y ejecutar el servicio
        var userIdConPermiso = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await _permissionManager.SetForUserAsync(
                userIdConPermiso,
                SmartPantryPermissions.Metricas.VerCatalogoExterno,
                true);
        });

        var principalConPermiso = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, userIdConPermiso.ToString()),
            new Claim(AbpClaimTypes.UserName, "admin.metricas")
        }, "TestAuth"));

        using (_currentPrincipalAccessor.Change(principalConPermiso))
        {
            (await realPermissionChecker.IsGrantedAsync(SmartPantryPermissions.Metricas.VerCatalogoExterno))
                .ShouldBeTrue();

            await realAuthService.CheckAsync(SmartPantryPermissions.Metricas.VerCatalogoExterno);

            var resumen = await _metricasAppService.GetResumenAsync(new GetMetricasCatalogoExternoInput());
            resumen.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetResumenAsync_Deberia_Calcular_Agregaciones_Desglose_Por_Operacion_Y_Serie_Diaria()
    {
        var dia1 = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);
        var dia2 = new DateTime(2026, 11, 26, 14, 0, 0, DateTimeKind.Utc);

        await WithUnitOfWorkAsync(async () =>
        {
            await _registroRepository.InsertManyAsync(new List<RegistroLlamadaExterna>
            {
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia1, 200, ResultadoLlamadaExterna.Success, 200),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia1.AddHours(1), 300, ResultadoLlamadaExterna.NotFound, 404),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorTexto, dia2, 500, ResultadoLlamadaExterna.Success, 200),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, dia2.AddMinutes(10), 1000, ResultadoLlamadaExterna.RateLimited, 429),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorTexto, dia2.AddMinutes(20), 2000, ResultadoLlamadaExterna.Timeout, null)
            }, autoSave: true);
        });

        var resumen = await _metricasAppService.GetResumenAsync(new GetMetricasCatalogoExternoInput
        {
            FechaDesdeUtc = dia1.AddHours(-1),
            FechaHastaUtc = dia2.AddHours(1)
        });

        resumen.TotalLlamadas.ShouldBe(5);
        resumen.LlamadasExitosas.ShouldBe(2);
        resumen.LlamadasNoEncontradas.ShouldBe(1);
        resumen.LlamadasRateLimited.ShouldBe(1);
        resumen.LlamadasTimeout.ShouldBe(1);
        resumen.LlamadasError.ShouldBe(0);

        resumen.PorcentajeExito.ShouldBe(40.0);
        resumen.PorcentajeError.ShouldBe(40.0);

        resumen.LatenciaPromedioMs.ShouldBe(800.0);
        resumen.LatenciaP95Ms.ShouldBe(2000);
        resumen.LatenciaMaximaMs.ShouldBe(2000);

        // Desglose por operación
        resumen.PorOperacion.Count.ShouldBe(2);

        var opCodigo = resumen.PorOperacion.Single(x => x.TipoOperacion == TipoOperacionCatalogoExterno.BusquedaPorCodigo);
        opCodigo.TotalLlamadas.ShouldBe(3);
        opCodigo.Exitosas.ShouldBe(1);
        opCodigo.NoEncontradas.ShouldBe(1);
        opCodigo.Fallidas.ShouldBe(1);
        opCodigo.LatenciaPromedioMs.ShouldBe(500.0);

        var opTexto = resumen.PorOperacion.Single(x => x.TipoOperacion == TipoOperacionCatalogoExterno.BusquedaPorTexto);
        opTexto.TotalLlamadas.ShouldBe(2);
        opTexto.Exitosas.ShouldBe(1);
        opTexto.NoEncontradas.ShouldBe(0);
        opTexto.Fallidas.ShouldBe(1);
        opTexto.LatenciaPromedioMs.ShouldBe(1250.0);

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
    public async Task GetResumenAsync_Deberia_Filtrar_Por_TipoOperacion_Y_Excluir_Registros_Fuera_De_Rango()
    {
        var baseFecha = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        await WithUnitOfWorkAsync(async () =>
        {
            await _registroRepository.InsertManyAsync(new List<RegistroLlamadaExterna>
            {
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, baseFecha, 150, ResultadoLlamadaExterna.Success, 200),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorTexto, baseFecha.AddMinutes(5), 300, ResultadoLlamadaExterna.Success, 200),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, baseFecha.AddDays(-2), 400, ResultadoLlamadaExterna.Error, 500),
                new(Guid.NewGuid(), TipoOperacionCatalogoExterno.BusquedaPorCodigo, baseFecha.AddDays(2), 500, ResultadoLlamadaExterna.Error, 500)
            }, autoSave: true);
        });

        var resumen = await _metricasAppService.GetResumenAsync(new GetMetricasCatalogoExternoInput
        {
            FechaDesdeUtc = baseFecha.AddHours(-1),
            FechaHastaUtc = baseFecha.AddHours(1),
            TipoOperacion = TipoOperacionCatalogoExterno.BusquedaPorCodigo
        });

        resumen.TotalLlamadas.ShouldBe(1);
        resumen.LlamadasExitosas.ShouldBe(1);
        resumen.LlamadasError.ShouldBe(0);
        resumen.LatenciaPromedioMs.ShouldBe(150.0);
        resumen.PorOperacion.ShouldHaveSingleItem().TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorCodigo);
    }

    [Fact]
    public async Task GetResumenAsync_Deberia_Devolver_Totales_En_Cero_Cuando_No_Hay_Registros_En_Rango()
    {
        var resumen = await _metricasAppService.GetResumenAsync(new GetMetricasCatalogoExternoInput
        {
            FechaDesdeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaHastaUtc = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        });

        resumen.TotalLlamadas.ShouldBe(0);
        resumen.PorcentajeExito.ShouldBe(0);
        resumen.PorcentajeError.ShouldBe(0);
        resumen.LatenciaPromedioMs.ShouldBe(0);
        resumen.PorOperacion.ShouldBeEmpty();
        resumen.SerieDiaria.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetResumenAsync_Deberia_Usar_Rango_Por_Defecto_De_7_Dias_Cuando_Fechas_Son_Null()
    {
        var resumen = await _metricasAppService.GetResumenAsync(new GetMetricasCatalogoExternoInput());

        (resumen.FechaHastaUtc - resumen.FechaDesdeUtc).TotalDays.ShouldBe(7, tolerance: 0.01);
        resumen.FechaDesdeUtc.Kind.ShouldBe(DateTimeKind.Utc);
        resumen.FechaHastaUtc.Kind.ShouldBe(DateTimeKind.Utc);
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