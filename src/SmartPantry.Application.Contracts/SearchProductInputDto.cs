using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos
{
    public class SearchProductInputDto
    {
        [Required(ErrorMessage = "El código de barras es requerido.")]
        [RegularExpression(@"^\d{8,14}$", ErrorMessage = "El código de barras debe ser numérico y contener entre 8 y 14 dígitos.")]
        public string CodigoBarras { get; set; } = default!;
    }
}
