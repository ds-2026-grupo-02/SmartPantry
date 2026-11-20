using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Despensa;

public class CreateDespensaDto
{
    [Required]
    public Guid ProductoId { get; set; }

    public DateTime? FechaVencimiento { get; set; }
}
