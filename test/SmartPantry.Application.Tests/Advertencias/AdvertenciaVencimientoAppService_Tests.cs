using NSubstitute;
using Shouldly;
using SmartPantry.Despensa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Timing;
using Volo.Abp.Users;
using Xunit;

namespace SmartPantry.Advertencias;

public class AdvertenciaVencimientoAppService_Tests
{
    private readonly Guid _usuarioActualId = Guid.NewGuid();
    private readonly Guid _otroUsuarioId = Guid.NewGuid();
    private readonly DateTime _fechaReferencia = new(2026, 11, 26, 14, 0, 0);

    private (AdvertenciaVencimientoAppService Service, List<AdvertenciaVencimiento> Store) CrearServicio(
        List<AdvertenciaVencimiento>? iniciales = null)
    {
        var store = iniciales ?? new List<AdvertenciaVencimiento>();

        var repo = Substitute.For<IRepository<AdvertenciaVencimiento, Guid>>();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(_usuarioActualId);

        var clock = Substitute.For<IClock>();
        clock.Now.Returns(_fechaReferencia);

        var asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();
        asyncExecuter.ToListAsync(Arg.Any<IQueryable<AdvertenciaVencimiento>>())
            .Returns(call => Task.FromResult(call.Arg<IQueryable<AdvertenciaVencimiento>>().ToList()));

        repo.GetQueryableAsync().Returns(call => Task.FromResult(store.AsQueryable()));
        repo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = call.Arg<Guid>();
                return Task.FromResult(store.FirstOrDefault(x => x.Id == id));
            });
        repo.UpdateAsync(Arg.Any<AdvertenciaVencimiento>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<AdvertenciaVencimiento>()));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICurrentUser)).Returns(currentUser);
        serviceProvider.GetService(typeof(IClock)).Returns(clock);
        serviceProvider.GetService(typeof(IAsyncQueryableExecuter)).Returns(asyncExecuter);

        var service = new AdvertenciaVencimientoAppService(repo)
        {
            LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider)
        };

        return (service, store);
    }

    [Fact]
    public async Task Deberia_Listar_Solo_Advertencias_Activas_Del_Usuario_Actual()
    {
        var activaPropia = new AdvertenciaVencimiento(
            Guid.NewGuid(), _usuarioActualId, Guid.NewGuid(), "VencimientoProximo", _fechaReferencia.AddDays(1));

        var inactivaPropia = new AdvertenciaVencimiento(
            Guid.NewGuid(), _usuarioActualId, Guid.NewGuid(), "VencimientoProximo", _fechaReferencia.AddDays(2));
        inactivaPropia.Desactivar();

        var activaDeOtroUsuario = new AdvertenciaVencimiento(
            Guid.NewGuid(), _otroUsuarioId, Guid.NewGuid(), "VencimientoProximo", _fechaReferencia.AddDays(1));

        var (service, _) = CrearServicio(new List<AdvertenciaVencimiento>
        {
            activaPropia,
            inactivaPropia,
            activaDeOtroUsuario
        });

        var resultado = await service.GetActivasAsync();

        resultado.Items.Count.ShouldBe(1);
        resultado.Items.Single().Id.ShouldBe(activaPropia.Id);
        resultado.Items.Single().EstadoVencimiento.ShouldBe(EstadoVencimiento.ProximoAVencer);
    }

    [Fact]
    public async Task Deberia_Marcar_Como_Vista_Advertencia_Propia()
    {
        var advertenciaPropia = new AdvertenciaVencimiento(
            Guid.NewGuid(), _usuarioActualId, Guid.NewGuid(), "VencimientoProximo", _fechaReferencia.AddDays(1));

        var (service, _) = CrearServicio(new List<AdvertenciaVencimiento> { advertenciaPropia });

        await service.MarcarComoVistaAsync(advertenciaPropia.Id);

        advertenciaPropia.EsActiva.ShouldBeFalse();
    }

    [Fact]
    public async Task Deberia_Lanzar_Excepcion_Al_Intentar_Marcar_Advertencia_De_Otro_Usuario()
    {
        var advertenciaAjena = new AdvertenciaVencimiento(
            Guid.NewGuid(), _otroUsuarioId, Guid.NewGuid(), "VencimientoProximo", _fechaReferencia.AddDays(1));

        var (service, _) = CrearServicio(new List<AdvertenciaVencimiento> { advertenciaAjena });

        await Should.ThrowAsync<EntityNotFoundException>(() =>
            service.MarcarComoVistaAsync(advertenciaAjena.Id));

        advertenciaAjena.EsActiva.ShouldBeTrue();
    }
}