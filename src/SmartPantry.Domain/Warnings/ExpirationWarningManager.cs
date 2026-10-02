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
        // 1. Consultar ítems de despensa no consumidos y con fecha de vencimiento definida
        var items = await _pantryItemRepository.GetListAsync(x => !x.EsConsumido && x.FechaVencimiento.HasValue);

        foreach (var item in items)
        {
            var expirationDate = item.FechaVencimiento!.Value.Date;
            var daysUntilExpiration = (expirationDate - referenceDateUtc.Date).Days;
            const string warningType = "VencimientoProximo";

            // Buscar la advertencia existente directamente de forma asíncrona
            var existingWarning = await _expirationWarningRepository.FirstOrDefaultAsync(
                x => x.PantryItemId == item.Id && x.WarningType == warningType
            );

            // 2. Si está dentro del umbral (ej. entre 0 y 3 días de vencer)
            if (daysUntilExpiration <= thresholdDays && daysUntilExpiration >= 0)
            {
                if (existingWarning == null)
                {
                    // Crear nueva advertencia (autoSave: true fuerza a persistir los cambios inmediatamente para mantener idempotencia en BD)
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
                    // Recomponer/Actualizar la advertencia si cambió la fecha o estaba inactiva
                    existingWarning.Reactivate(item.FechaVencimiento);
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
            }
            else
            {
                // 3. Si ya no está dentro del umbral o cambió de fecha lejana, desactivarla
                if (existingWarning != null && existingWarning.IsActive)
                {
                    existingWarning.Deactivate();
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
            }
        }
    }
}