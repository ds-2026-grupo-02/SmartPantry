using System;
using System.Linq;
using System.Threading.Tasks;
using SmartPantry.PantryItems;
using SmartPantry.Warnings;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Warnings;

public class ExpirationWarningManager_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly ExpirationWarningManager _warningManager;
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<ExpirationWarning, Guid> _expirationWarningRepository;

    public ExpirationWarningManager_Tests()
    {
        _warningManager = GetRequiredService<ExpirationWarningManager>();
        _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
        _expirationWarningRepository = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
    }

   
    [Fact]
    public async Task Should_Create_Warning_When_Item_Is_Within_Threshold()
    {
        var referenceDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var expirationDate = referenceDate.AddDays(2);

        var itemId = Guid.NewGuid();
        var item = new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), 1, "Unidad", expirationDate);

        await WithUnitOfWorkAsync(async () =>
        {
            await _pantryItemRepository.InsertAsync(item);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _warningManager.ProcessExpirationsAsync(referenceDate, thresholdDays: 3);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1);
            warnings.First().IsActive.ShouldBeTrue();
            warnings.First().WarningType.ShouldBe("VencimientoProximo");
        });
    }

    [Fact]
    public async Task Should_Be_Idempotent_When_Executed_Multiple_Times()
    {
        var referenceDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var expirationDate = referenceDate.AddDays(1);

        var itemId = Guid.NewGuid();
        var item = new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), 1, "Unidad", expirationDate);

        // Insertar el ítem inicial
        await WithUnitOfWorkAsync(async () =>
        {
            await _pantryItemRepository.InsertAsync(item);
        });

        // Primera corrida del manager
        await WithUnitOfWorkAsync(async () =>
        {
            await _warningManager.ProcessExpirationsAsync(referenceDate, thresholdDays: 3);
        });

        // Segunda corrida del manager (debe ser idempotente)
        await WithUnitOfWorkAsync(async () =>
        {
            await _warningManager.ProcessExpirationsAsync(referenceDate, thresholdDays: 3);
        });

        // Verificación
        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Deactivate_Warning_When_Expiration_Date_Is_Updated_Outside_Threshold()
    {
        var referenceDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var initialExpiration = referenceDate.AddDays(1);

        var itemId = Guid.NewGuid();
        var item = new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), 1, "Unidad", initialExpiration);

        await WithUnitOfWorkAsync(async () =>
        {
            await _pantryItemRepository.InsertAsync(item);
        });

        // Generamos la advertencia inicial
        await WithUnitOfWorkAsync(async () =>
        {
            await _warningManager.ProcessExpirationsAsync(referenceDate, thresholdDays: 3);
        });

        // Modificamos el item existente y procesamos nuevamente
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _pantryItemRepository.GetAsync(itemId);
            dbItem.UpdateExpirationDate(referenceDate.AddDays(20));
            await _pantryItemRepository.UpdateAsync(dbItem);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _warningManager.ProcessExpirationsAsync(referenceDate, thresholdDays: 3);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1);
            warnings.First().IsActive.ShouldBeFalse();
        });
    }
}