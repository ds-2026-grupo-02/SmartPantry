using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Perfil;

public class ActualizarPerfilDto
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }
}
