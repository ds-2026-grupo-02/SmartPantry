using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Perfil;

public class ActualizarFotoDto
{
    [Required]
    public string FotoBase64 { get; set; } = string.Empty;
}
