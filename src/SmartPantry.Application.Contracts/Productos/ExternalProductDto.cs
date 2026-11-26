using System;

namespace SmartPantry.Productos;

public class ExternalProductDto
{
    public string? Nombre { get; set; }
    public string? NombreEs { get; set; }
    public string? Cantidad { get; set; }
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
    public string? CodigoBarras { get; set; }
    public string? Categorias { get; set; }
    public string? Ingredientes { get; set; }
    public string? Alergenos { get; set; }
    public string? NutriScore { get; set; }
    public int? Nova { get; set; }
}
