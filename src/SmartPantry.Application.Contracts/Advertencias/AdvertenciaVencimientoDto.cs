using SmartPantry.Despensa;
using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Advertencias;

public class AdvertenciaVencimientoDto : EntityDto<Guid>
{
    public Guid UsuarioId { get; set; }
    public Guid ItemDespensaId { get; set; }
    public string TipoAdvertencia { get; set; } = string.Empty;
    public bool EsActiva { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public EstadoVencimiento EstadoVencimiento { get; set; }
}
