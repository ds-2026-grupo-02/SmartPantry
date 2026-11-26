using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Despensa;

public class DespensaDto : EntityDto<Guid>
{
    public Guid UsuarioId { get; set; }
    public Guid ProductoId { get; set; }
    public string? NombreProductoEs { get; set; }
    public string? NombreProductoEn { get; set; }
    public string? CantidadProducto { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool EsConsumido { get; set; }
    public EstadoVencimiento EstadoVencimiento { get; set; }
}
