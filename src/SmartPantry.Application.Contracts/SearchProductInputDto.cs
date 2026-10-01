using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos
{
    public class SearchProductInputDto
    {
        [Required] 
        public string CodigoBarras { get; set; } = default!;
    }
}
