using SmartPantry.Despensa;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Timing;

namespace SmartPantry.Advertencias;

public class GestorAdvertenciasVencimiento : DomainService
{
    public const string TipoVencimientoProximo = "VencimientoProximo";
    private readonly IRepository<ItemDespensa, Guid> _pantryItemRepository;
    private readonly IRepository<AdvertenciaVencimiento, Guid> _expirationWarningRepository;
    private readonly IClock _clock;

    public GestorAdvertenciasVencimiento(
        IRepository<ItemDespensa, Guid> pantryItemRepository,
        IRepository<AdvertenciaVencimiento, Guid> expirationWarningRepository,
        IClock clock)
    {
        _pantryItemRepository = pantryItemRepository;
        _expirationWarningRepository = expirationWarningRepository;
        _clock = clock;
    }

    public async Task ProcessExpirationsAsync(DateTime? referenceDate = null, UmbralVencimiento? umbral = null)
    {
        var fechaReferencia = (referenceDate ?? _clock.Now).Date;
        var umbralEfectivo = umbral ?? UmbralVencimiento.PorDefecto();

        // 1. Consultar todos los ítems de despensa para evaluar activos, consumidos o sin fecha
        var items = await _pantryItemRepository.GetListAsync();

        foreach (var item in items)
        {
            // Buscar la advertencia existente directamente de forma asíncrona
            var existingWarning = await _expirationWarningRepository.FirstOrDefaultAsync(
                x => x.ItemDespensaId == item.Id && x.TipoAdvertencia == TipoVencimientoProximo
            );

            // 2. Si el ítem está consumido o no tiene fecha de vencimiento definida, no debe tener advertencia activa
            if (item.EsConsumido || !item.FechaVencimiento.HasValue)
            {
                if (existingWarning != null && existingWarning.EsActiva)
                {
                    existingWarning.Desactivar();
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
                continue;
            }

            var expirationDate = item.FechaVencimiento.Value.Date;
            var daysUntilExpiration = (expirationDate - fechaReferencia).Days;

            // 3. Si está dentro del umbral (ej. entre 0 y 3 días de vencer, considerando día límite)
            if (daysUntilExpiration <= umbralEfectivo.Dias && daysUntilExpiration >= 0)
            {
                if (existingWarning == null)
                {
                    var newWarning = new AdvertenciaVencimiento(
                        GuidGenerator.Create(),
                        item.UsuarioId,
                        item.Id,
                        TipoVencimientoProximo,
                        item.FechaVencimiento
                    );
                    await _expirationWarningRepository.InsertAsync(newWarning, autoSave: true);
                }
                else if (!existingWarning.EsActiva || existingWarning.FechaVencimiento != item.FechaVencimiento)
                {
                    existingWarning.Reactivar(item.FechaVencimiento);
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
            }
            else
            {
                // 4. Si ya no está dentro del umbral (ej. vencido < 0 o fecha lejana > umbralEfectivo.Dias): desactivar advertencia
                if (existingWarning != null && existingWarning.EsActiva)
                {
                    existingWarning.Desactivar();
                    await _expirationWarningRepository.UpdateAsync(existingWarning, autoSave: true);
                }
            }
        }
    }
}