using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Productos;

public class ProductoDto : EntityDto<Guid>
{
    public string Nombre { get; set; } = default!;
    public string CodigoBarras { get; set; } = default!;

    // Constructor sin parámetros requerido por las herramientas de mapeo
    public ProductoDto() { }
}