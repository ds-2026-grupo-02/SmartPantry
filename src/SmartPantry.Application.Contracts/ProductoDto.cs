using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Productos;

public class ProductoDto : EntityDto<Guid>
{
    public string? NombreEs { get; set; }
    public string? NombreEn { get; set; }
    public string? Cantidad { get; set; }
    public string CodigoBarras { get; set; } = default!;

    // Constructor sin parámetros requerido por las herramientas de mapeo
    public ProductoDto() { }
}
