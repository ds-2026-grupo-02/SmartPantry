using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class CreateUpdateProductoDto
{
    [Required]
    [StringLength(ProductoConsts.MaxNombreLength)]
    public string Nombre { get; set; } = default!;

    [Required]
    [StringLength(ProductoConsts.MaxCodigoBarrasLength)]
    public string CodigoBarras { get; set; } = default!;
}