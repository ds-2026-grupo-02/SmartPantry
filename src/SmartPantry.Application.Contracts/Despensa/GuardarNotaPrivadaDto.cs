using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Despensa;

public class GuardarNotaPrivadaDto
{
    [Required]
    [StringLength(500)]
    public string Contenido { get; set; } = string.Empty;
}


//para recibir el texto al crear o editar una nota