using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.PantryItems;

public class CreateDespensaDto
{
    [Required]
    public Guid ProductoId { get; set; }

    public DateTime? FechaVencimiento { get; set; }
}
