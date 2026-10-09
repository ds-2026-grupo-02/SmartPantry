using System;
using System.Linq;
using System.Threading.Tasks;
using SmartPantry.PantryItems;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace SmartPantry.Warnings;

public class ExpirationWarningManager : DomainService
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<ExpirationWarning, Guid> _expirationWarningRepository;

    public ExpirationWarningManager(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IRepository<ExpirationWarning, Guid> expirationWarningRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _expirationWarningRepository = expirationWarningRepository;
    }

    public async Task ProcessExpirationsAsync(DateTime referenceDateUtc, int thresholdDays = 3)
    {
        // 1. Consultar todos los ítems de despensa para evaluar activos, consumidos o sin fecha
        var items = await _pantryItemRepository.GetListAsync();
        const string warningType = "VencimientoProximo";

        foreach (var item in items)
        {
            // Buscar la advertencia existente directamente de forma asíncrona
            var existingWarning = await _expirationWarningRepository.FirstOrDefaultAsync(
                x => x.PantryItemId == item.Id && x.WarningType == warningType
            );

            // 2. Si el ítem está consumido o no tiene fecha de vencimiento definida, no debe tener advertencia activa
            if (item.EsConsumido || !item.FechaVencimiento.HasValue)
            {
                if (existingWarning != null && existingWarning.IsActive)
                {
                    existingWarning.Deactivate();
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
                continue;
            }

            var expirationDate = item.FechaVencimiento.Value.Date;
            var daysUntilExpiration = (expirationDate - referenceDateUtc.Date).Days;

            // 3. Si está dentro del umbral (ej. entre 0 y 3 días de vencer, considerando día límite)
            if (daysUntilExpiration <= thresholdDays && daysUntilExpiration >= 0)
            {
                if (existingWarning == null)
                {
                    var newWarning = new ExpirationWarning(
                        GuidGenerator.Create(),
                        item.UserId,
                        item.Id,
                        warningType,
                        item.FechaVencimiento
                    );
                    await _expirationWarningRepository.InsertAsync(newWarning, autoSave: true);
                }
                else if (!existingWarning.IsActive || existingWarning.ExpirationDate != item.FechaVencimiento)
                {
                    existingWarning.Reactivate(item.FechaVencimiento);
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
            }
            else
            {
                // 4. Si ya no está dentro del umbral (ej. vencido < 0 o fecha lejana > thresholdDays)
                if (existingWarning != null && existingWarning.IsActive)
                {
                    existingWarning.Deactivate();
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
            }
        }
    }
}