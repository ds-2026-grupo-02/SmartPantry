using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Productos;

public class ProductoDto : EntityDto<Guid>
{
    public string? NombreEs { get; set; }
    public string? NombreEn { get; set; }
    public string? Cantidad { get; set; }
    public string CodigoBarras { get; set; } = default!;
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Categorias { get; set; }
    public string? Ingredientes { get; set; }
    public string? Alergenos { get; set; }
    public string? NutriScore { get; set; }
    public int? Nova { get; set; }

    // Constructor sin parámetros requerido por las herramientas de mapeo
    public ProductoDto() { }
}
