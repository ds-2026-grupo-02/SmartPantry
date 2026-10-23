using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.PantryItems;

public class DespensaDto : EntityDto<Guid>
{
    public Guid UserId { get; set; }
    public Guid ProductoId { get; set; }
    public string NombreProducto { get; set; } = default!;
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = default!;
    public DateTime? FechaVencimiento { get; set; }
    public bool EsConsumido { get; set; }
}