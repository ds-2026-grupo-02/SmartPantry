using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.PantryItems;
using SmartPantry.Productos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Applications;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class DespensaAppServiceSecurityTests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IDespensaAppService _despensaAppService;
    private readonly IRepository<Producto, Guid> _productoRepository;
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public DespensaAppServiceSecurityTests()
    {
        _despensaAppService = GetRequiredService<IDespensaAppService>();
        _productoRepository = GetRequiredService<IRepository<Producto, Guid>>();
        _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private IDisposable ChangeUser(Guid userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, "test-" + userId),
            new Claim(AbpClaimTypes.Email, $"test-{userId}@test.com")
        }, "FakeAuth"));

        return _currentPrincipalAccessor.Change(principal);
    }

    [Fact]
    public async Task No_Deberia_Permitir_Consultar_Items_De_Otro_Usuario()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var producto = await _productoRepository.InsertAsync(
            new Producto(Guid.NewGuid(), "7790000000001", "Arroz", "Rice", "1 kg")
        );

        // Usuario 1 agrega un ítem a su despensa
        Guid itemUser1Id;
        using (ChangeUser(user1Id))
        {
            var creado = await _despensaAppService.CreateAsync(new CreateDespensaDto
            {
                ProductoId = producto.Id,
                FechaVencimiento = DateTime.Today.AddDays(30)
            });
            itemUser1Id = creado.Id;

            // Usuario 1 sí puede ver su propio ítem
            var listaUser1 = await _despensaAppService.GetListAsync(new PagedAndSortedResultRequestDto());
            listaUser1.Items.ShouldContain(x => x.Id == itemUser1Id);
        }

        // Act & Assert: Usuario 2 consulta su despensa
        using (ChangeUser(user2Id))
        {
            var listaUser2 = await _despensaAppService.GetListAsync(new PagedAndSortedResultRequestDto());

            // La consulta de Usuario 2 NO debe incluir el ítem del Usuario 1
            listaUser2.Items.ShouldNotContain(x => x.Id == itemUser1Id);
            listaUser2.TotalCount.ShouldBe(0);
        }
    }

    [Fact]
    public async Task No_Deberia_Permitir_Modificar_Item_De_Otro_Usuario()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var fechaOriginal = DateTime.Today.AddDays(10);
        var fechaModificada = DateTime.Today.AddDays(20);

        var producto = await _productoRepository.InsertAsync(
            new Producto(Guid.NewGuid(), "7790000000002", "Leche", "Milk", "1 L")
        );

        Guid itemUser1Id;
        using (ChangeUser(user1Id))
        {
            var creado = await _despensaAppService.CreateAsync(new CreateDespensaDto
            {
                ProductoId = producto.Id,
                FechaVencimiento = fechaOriginal
            });
            itemUser1Id = creado.Id;
        }

        // Act & Assert: Usuario 2 intenta modificar el ítem de Usuario 1
        using (ChangeUser(user2Id))
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _despensaAppService.UpdateAsync(itemUser1Id, new UpdateDespensaDto
                {
                    FechaVencimiento = fechaModificada
                });
            });
        }

        // Verificar que el ítem de Usuario 1 no fue alterado
        var itemEnDb = await _pantryItemRepository.GetAsync(itemUser1Id);
        itemEnDb.FechaVencimiento.ShouldBe(fechaOriginal);
    }

    [Fact]
    public async Task No_Deberia_Permitir_Marcar_Consumido_Ni_Eliminar_Item_De_Otro_Usuario()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var producto = await _productoRepository.InsertAsync(
            new Producto(Guid.NewGuid(), "7790000000003", "Fideos", "Pasta", "500 g")
        );

        Guid itemUser1Id;
        using (ChangeUser(user1Id))
        {
            var creado = await _despensaAppService.CreateAsync(new CreateDespensaDto
            {
                ProductoId = producto.Id,
                FechaVencimiento = DateTime.Today.AddDays(15)
            });
            itemUser1Id = creado.Id;
        }

        // Act & Assert: Usuario 2 intenta consumirlo y eliminarlo
        using (ChangeUser(user2Id))
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _despensaAppService.MarcarConsumidoAsync(itemUser1Id);
            });

            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _despensaAppService.DeleteAsync(itemUser1Id);
            });
        }

        // Verificar que el ítem sigue existiendo y no está consumido
        var itemEnDb = await _pantryItemRepository.GetAsync(itemUser1Id);
        itemEnDb.EsConsumido.ShouldBeFalse();
    }
}
