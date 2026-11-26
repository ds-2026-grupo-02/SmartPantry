using NSubstitute;
using Shouldly;
using SmartPantry.Productos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;
using Volo.Abp.Users;
using Xunit;

namespace SmartPantry.Despensa;

public class Vencimiento_Tests
{
    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly Guid _productoId = Guid.NewGuid();
    private readonly DateTime _fechaReferencia = new(2026, 11, 26, 14, 0, 0);

    private (DespensaAppService Service, List<ItemDespensa> Items) CrearServicio(
        List<ItemDespensa>? itemsIniciales = null)
    {
        var items = itemsIniciales ?? new List<ItemDespensa>();

        var itemRepo = Substitute.For<IRepository<ItemDespensa, Guid>>();
        var productoRepo = Substitute.For<IRepository<Producto, Guid>>();
        var notaRepo = Substitute.For<IRepository<NotaPrivadaDespensa, Guid>>();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(_usuarioId);

        var clock = Substitute.For<IClock>();
        clock.Now.Returns(_fechaReferencia);

        var guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());

        var objectMapper = Substitute.For<IObjectMapper>();
        objectMapper.Map<ItemDespensa, DespensaDto>(Arg.Any<ItemDespensa>())
            .Returns(call =>
            {
                var src = call.Arg<ItemDespensa>();
                return new DespensaDto
                {
                    Id = src.Id,
                    UsuarioId = src.UsuarioId,
                    ProductoId = src.ProductoId,
                    FechaVencimiento = src.FechaVencimiento,
                    EsConsumido = src.EsConsumido
                };
            });

        var asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();
        asyncExecuter.CountAsync(Arg.Any<IQueryable<ItemDespensa>>())
            .Returns(call => Task.FromResult(call.Arg<IQueryable<ItemDespensa>>().Count()));
        asyncExecuter.ToListAsync(Arg.Any<IQueryable<ItemDespensa>>())
            .Returns(call => Task.FromResult(call.Arg<IQueryable<ItemDespensa>>().ToList()));

        var producto = new Producto(_productoId, "Leche", "Milk", "1L");
        productoRepo.GetAsync(_productoId).Returns(Task.FromResult(producto));
        productoRepo.FindAsync(_productoId).Returns(Task.FromResult<Producto?>(producto));

        itemRepo.GetQueryableAsync().Returns(call => Task.FromResult(items.AsQueryable()));
        itemRepo.InsertAsync(Arg.Any<ItemDespensa>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var nuevo = call.Arg<ItemDespensa>();
                items.Add(nuevo);
                return Task.FromResult(nuevo);
            });
        itemRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = call.Arg<Guid>();
                return Task.FromResult(items.FirstOrDefault(x => x.Id == id));
            });
        itemRepo.UpdateAsync(Arg.Any<ItemDespensa>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<ItemDespensa>()));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICurrentUser)).Returns(currentUser);
        serviceProvider.GetService(typeof(IClock)).Returns(clock);
        serviceProvider.GetService(typeof(IGuidGenerator)).Returns(guidGenerator);
        serviceProvider.GetService(typeof(IObjectMapper)).Returns(objectMapper);
        serviceProvider.GetService(typeof(IAsyncQueryableExecuter)).Returns(asyncExecuter);

        var lazyProvider = new AbpLazyServiceProvider(serviceProvider);

        var service = new DespensaAppService(itemRepo, productoRepo, notaRepo)
        {
            LazyServiceProvider = lazyProvider
        };

        return (service, items);
    }

    [Theory]
    [InlineData(null, EstadoVencimiento.SinFecha)]
    [InlineData(10, EstadoVencimiento.Vigente)]
    [InlineData(3, EstadoVencimiento.ProximoAVencer)]
    [InlineData(1, EstadoVencimiento.ProximoAVencer)]
    [InlineData(0, EstadoVencimiento.ProximoAVencer)]
    [InlineData(-1, EstadoVencimiento.Vencido)]
    public async Task Deberia_Calcular_EstadoVencimiento_En_Create_Y_GetListAsync(
        int? diasDesfasaje,
        EstadoVencimiento estadoEsperado)
    {
        var (service, _) = CrearServicio();

        DateTime? fechaVencimiento = diasDesfasaje.HasValue
            ? _fechaReferencia.Date.AddDays(diasDesfasaje.Value)
            : null;

        var creado = await service.CreateAsync(new CreateDespensaDto
        {
            ProductoId = _productoId,
            FechaVencimiento = fechaVencimiento
        });

        creado.EstadoVencimiento.ShouldBe(estadoEsperado);

        var listado = await service.GetListAsync(new GetDespensaListInput());
        var itemEnLista = listado.Items.Single(x => x.Id == creado.Id);
        itemEnLista.EstadoVencimiento.ShouldBe(estadoEsperado);
    }

    [Fact]
    public async Task Deberia_Actualizar_EstadoVencimiento_Al_Modificar_Fecha_En_UpdateAsync()
    {
        var (service, _) = CrearServicio();

        var creado = await service.CreateAsync(new CreateDespensaDto
        {
            ProductoId = _productoId,
            FechaVencimiento = _fechaReferencia.Date.AddDays(10)
        });
        creado.EstadoVencimiento.ShouldBe(EstadoVencimiento.Vigente);

        var actualizado = await service.UpdateAsync(creado.Id, new UpdateDespensaDto
        {
            FechaVencimiento = _fechaReferencia.Date.AddDays(-1)
        });
        actualizado.EstadoVencimiento.ShouldBe(EstadoVencimiento.Vencido);
    }
}