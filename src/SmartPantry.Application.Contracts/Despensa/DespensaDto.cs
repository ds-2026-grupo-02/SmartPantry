using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.PantryItems;

public class DespensaDto : EntityDto<Guid>
{
    public Guid UserId { get; set; }
    public Guid ProductoId { get; set; }
    public string? NombreProductoEs { get; set; }
    public string? NombreProductoEn { get; set; }
    public string? CantidadProducto { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool EsConsumido { get; set; }
}
