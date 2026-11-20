using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class CreateUpdateProductoDto : IValidatableObject
{
    [StringLength(ProductoConsts.MaxNombreLength)]
    public string? NombreEs { get; set; }

    [StringLength(ProductoConsts.MaxNombreLength)]
    public string? NombreEn { get; set; }

    [StringLength(ProductoConsts.MaxCantidadLength)]
    public string? Cantidad { get; set; }

    [Required]
    [StringLength(ProductoConsts.MaxCodigoBarrasLength)]
    public string CodigoBarras { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NombreEs) && string.IsNullOrWhiteSpace(NombreEn))
        {
            yield return new ValidationResult(
                "El producto debe tener al menos un nombre (en español o en inglés).",
                new[] { nameof(NombreEs), nameof(NombreEn) }
            );
        }
    }
}
