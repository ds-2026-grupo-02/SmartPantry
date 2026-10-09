using Shouldly;
using SmartPantry.PantryItems;
using SmartPantry.Productos;
using SmartPantry.Warnings;
using System;
using System.Linq;
using System.Threading.Tasks;
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

    // 1. REQUERIMIENTO: Probar fecha controlada UTC (Dentro/Fuera de umbral, Día límite, Vencido)
    [Theory]
    [InlineData(2, true)]   // Dentro del umbral (2 días antes) -> Genera advertencia
    [InlineData(0, true)]   // Día límite (vence hoy) -> Genera advertencia
    [InlineData(-1, false)] // Vencido (venció ayer) -> No genera advertencia
    [InlineData(10, false)] // Fuera del umbral (vence en 10 días) -> No genera advertencia
    public async Task Should_Handle_Different_Expiration_Thresholds(int offsetDays, bool shouldBeActive)
    {
        var referenceDateUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var expirationDateUtc = referenceDateUtc.AddDays(offsetDays);
        var itemId = Guid.NewGuid();

        var item = new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), 1, "Unidad", expirationDateUtc);

        await WithUnitOfWorkAsync(async () => await _pantryItemRepository.InsertAsync(item));

        await WithUnitOfWorkAsync(async () =>
            await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            if (shouldBeActive)
            {
                warnings.Count.ShouldBe(1);
                warnings.First().IsActive.ShouldBeTrue();
            }
            else
            {
                warnings.Count(x => x.IsActive).ShouldBe(0);
            }
        });
    }

    // 2. REQUERIMIENTO: Casos de borde (Sin fecha y Consumido)
    [Fact]
    public async Task Should_Ignore_Consumed_Or_Undated_Items()
    {
        var referenceDateUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);

        // Ítem sin fecha de vencimiento
        var undatedItem = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1m, "Unidad", null);

        // Ítem consumido (invocando el método de dominio)
        var consumedItem = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1m, "Unidad", referenceDateUtc.AddDays(1));
        consumedItem.MarcarComoConsumido();

        await WithUnitOfWorkAsync(async () =>
        {
            await _pantryItemRepository.InsertAsync(undatedItem);
            await _pantryItemRepository.InsertAsync(consumedItem);
        });

        await WithUnitOfWorkAsync(async () =>
            await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var warningsUndated = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == undatedItem.Id);
            var warningsConsumed = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == consumedItem.Id);

            warningsUndated.Count.ShouldBe(0);
            warningsConsumed.Count.ShouldBe(0);
        });
    }
    // 3. REQUERIMIENTO: Demostrar Idempotencia (Doble ejecución sobre los mismos datos)
    [Fact]
    public async Task Should_Be_Idempotent_When_Executed_Multiple_Times()
    {
        var referenceDateUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();
        var item = new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), 1, "Unidad", referenceDateUtc.AddDays(1));

        await WithUnitOfWorkAsync(async () => await _pantryItemRepository.InsertAsync(item));

        // Corrida 1
        await WithUnitOfWorkAsync(async () =>
            await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        // Corrida 2 (Ejecución repetida)
        await WithUnitOfWorkAsync(async () =>
            await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1); // Se mantiene una sola advertencia por ítem
        });
    }

    // 4. REQUERIMIENTO: Ciclo completo (Actualización, Desactivación y Reactivación)
    [Fact]
    public async Task Should_Deactivate_And_Reactivate_Same_Warning_When_Date_Changes()
    {
        var referenceDateUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();

        // Paso A: Crear ítem próximo a vencer -> genera advertencia activa
        var item = new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), 1, "Unidad", referenceDateUtc.AddDays(1));
        await WithUnitOfWorkAsync(async () => await _pantryItemRepository.InsertAsync(item));
        await WithUnitOfWorkAsync(async () => await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        // Paso B: Extender fecha fuera del umbral (+20 días) -> Desactivación
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _pantryItemRepository.GetAsync(itemId);
            dbItem.UpdateExpirationDate(referenceDateUtc.AddDays(20));
            await _pantryItemRepository.UpdateAsync(dbItem);
        });
        await WithUnitOfWorkAsync(async () => await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1);
            warnings.First().IsActive.ShouldBeFalse(); // Desactivada
        });

        // Paso C: Volver a acercar la fecha (+2 días) -> Reactivación de la MISMA advertencia
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _pantryItemRepository.GetAsync(itemId);
            dbItem.UpdateExpirationDate(referenceDateUtc.AddDays(2));
            await _pantryItemRepository.UpdateAsync(dbItem);
        });
        await WithUnitOfWorkAsync(async () => await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1); // Sigue habiendo un único registro
            warnings.First().IsActive.ShouldBeTrue(); // Reactivada
        });
    }

    // Cuando un producto con advertencia activa se marca como consumido,
    // la advertencia pasa efectivamente a IsActive = false.
    [Fact]
    public async Task Should_Deactivate_Warning_When_Item_Is_Marked_As_Consumed()
    {
        var referenceDateUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();

        // 1. Ítem activo dentro del umbral -> Genera advertencia
        var item = new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), 1, "Unidad", referenceDateUtc.AddDays(2));
        await WithUnitOfWorkAsync(async () => await _pantryItemRepository.InsertAsync(item));
        await WithUnitOfWorkAsync(async () => await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        // Verificamos que la advertencia esté activa
        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1);
            warnings.First().IsActive.ShouldBeTrue();
        });

        // 2. Se consume el ítem -> Se ejecuta el manager nuevamente
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _pantryItemRepository.GetAsync(itemId);
            dbItem.MarcarComoConsumido();
            await _pantryItemRepository.UpdateAsync(dbItem);
        });
        await WithUnitOfWorkAsync(async () => await _warningManager.ProcessExpirationsAsync(referenceDateUtc, thresholdDays: 3));

        // 3. Comprobamos que la advertencia fue desactivada
        await WithUnitOfWorkAsync(async () =>
        {
            var warnings = await _expirationWarningRepository.GetListAsync(x => x.PantryItemId == itemId);
            warnings.Count.ShouldBe(1);
            warnings.First().IsActive.ShouldBeFalse();
        });
    }
}