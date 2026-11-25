using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SmartPantry.Productos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Users;
using Xunit;

namespace SmartPantry.Despensa;

public class NotasPrivadas_Tests
{
    private readonly IRepository<ItemDespensa, Guid> _itemRepo = Substitute.For<IRepository<ItemDespensa, Guid>>();
    private readonly IRepository<Producto, Guid> _productoRepo = Substitute.For<IRepository<Producto, Guid>>();
    private readonly IRepository<NotaPrivadaDespensa, Guid> _notaRepo = Substitute.For<IRepository<NotaPrivadaDespensa, Guid>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IGuidGenerator _guidGen = Substitute.For<IGuidGenerator>();

    private void SetUsuario(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private DespensaAppService CrearService()
    {
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetRequiredService<ICurrentUser>().Returns(_ => _currentUser);
        lazy.LazyGetRequiredService<IGuidGenerator>().Returns(_ => _guidGen);

        return new DespensaAppService(_itemRepo, _productoRepo, _notaRepo)
        {
            LazyServiceProvider = lazy
        };
    }

    [Fact]
    public void NotaPrivada_Deberia_Crear_Y_Actualizar_Contenido()
    {
        var notaId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var nota = new NotaPrivadaDespensa(notaId, itemId, usuarioId, "Nota inicial");

        nota.Id.ShouldBe(notaId);
        nota.ItemDespensaId.ShouldBe(itemId);
        nota.UsuarioId.ShouldBe(usuarioId);
        nota.Contenido.ShouldBe("Nota inicial");

        nota.ActualizarContenido("Nota editada");
        nota.Contenido.ShouldBe("Nota editada");
    }
    [Fact]
    public async Task GuardarNotaPrivadaAsync_Deberia_Insertar_Cuando_No_Existe_Nota()
    {
        var usuarioId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        SetUsuario(usuarioId);

        var item = new ItemDespensa(itemId, usuarioId, Guid.NewGuid(), DateTime.UtcNow.AddDays(5));
        _itemRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), default)
            .Returns(Task.FromResult<ItemDespensa?>(item));

        _notaRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<NotaPrivadaDespensa, bool>>>())
            .Returns(Task.FromResult<NotaPrivadaDespensa?>(null));

        var service = CrearService();

        var resultado = await service.GuardarNotaPrivadaAsync(
            itemId,
            new GuardarNotaPrivadaDto { Contenido = "Mi nota" });

        resultado.ShouldNotBeNull();
        resultado.Contenido.ShouldBe("Mi nota");
        await _notaRepo.ReceivedWithAnyArgs(1).InsertAsync(default!, default, default);
    }

    [Fact]
    public async Task GetNotaPrivadaAsync_Deberia_Fallar_Si_El_Item_Es_De_Otro_Usuario()
    {
        var duenioId = Guid.NewGuid();
        var intrusoId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        SetUsuario(intrusoId);

        var itemDeOtro = new ItemDespensa(itemId, duenioId, Guid.NewGuid(), DateTime.UtcNow.AddDays(5));
        _itemRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), default)
            .Returns(Task.FromResult<ItemDespensa?>(itemDeOtro));

        var service = CrearService();

        await Should.ThrowAsync<EntityNotFoundException>(() => service.GetNotaPrivadaAsync(itemId));
    }
}