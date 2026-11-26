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

    [StringLength(ProductoConsts.MaxMarcaLength)]
    public string? Marca { get; set; }

    [StringLength(ProductoConsts.MaxImagenUrlLength)]
    public string? ImagenUrl { get; set; }

    [StringLength(ProductoConsts.MaxCategoriasLength)]
    public string? Categorias { get; set; }

    [StringLength(ProductoConsts.MaxIngredientesLength)]
    public string? Ingredientes { get; set; }

    [StringLength(ProductoConsts.MaxAlergenosLength)]
    public string? Alergenos { get; set; }

    [StringLength(ProductoConsts.MaxNutriScoreLength)]
    public string? NutriScore { get; set; }

    [Range(ProductoConsts.MinNova, ProductoConsts.MaxNova)]
    public int? Nova { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NombreEs) && string.IsNullOrWhiteSpace(NombreEn))
        {
            yield return new ValidationResult(
                "El producto debe tener al menos un nombre (en español o en inglés).",
                new[] { nameof(NombreEs), nameof(NombreEn) }
            );
        }

        if (!string.IsNullOrWhiteSpace(NutriScore))
        {
            var normalized = NutriScore.Trim().ToUpperInvariant();
            if (normalized.Length != 1 || normalized[0] < 'A' || normalized[0] > 'E')
            {
                yield return new ValidationResult(
                    "El Nutri-Score debe ser una letra entre A y E.",
                    new[] { nameof(NutriScore) }
                );
            }
        }
    }
}
