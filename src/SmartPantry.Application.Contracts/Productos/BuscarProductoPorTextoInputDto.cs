using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class BuscarProductoPorTextoInputDto : IValidatableObject
{
    public const int PaginaPorDefecto = 1;
    public const int TamanioPaginaPorDefecto = 20;
    public const int TamanioPaginaMaximo = 50;
    public const int VentanaMaximaResultados = 10000;

    [Required(ErrorMessage = "El texto de búsqueda es requerido.")]
    [StringLength(200, ErrorMessage = "El texto de búsqueda no puede superar los 200 caracteres.")]
    public string Texto { get; set; } = default!;

    [Range(1, VentanaMaximaResultados, ErrorMessage = "La página debe ser mayor o igual a 1.")]
    public int Pagina { get; set; } = PaginaPorDefecto;

    [Range(1, TamanioPaginaMaximo, ErrorMessage = "El tamaño de página debe estar entre 1 y 50.")]
    public int TamanioPagina { get; set; } = TamanioPaginaPorDefecto;

    // Códigos de idioma ISO-639-1 opcionales (ej. "es", "en"). Si es nulo o vacío, busca globalmente.
    public List<string>? Idiomas { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Texto))
        {
            yield return new ValidationResult(
                "El texto de búsqueda es requerido.",
                new[] { nameof(Texto) });
        }

        if ((long)Pagina * TamanioPagina > VentanaMaximaResultados)
        {
            yield return new ValidationResult(
                $"La paginación excede el límite máximo permitido ({VentanaMaximaResultados} resultados).",
                new[] { nameof(Pagina), nameof(TamanioPagina) });
        }
    }
}
