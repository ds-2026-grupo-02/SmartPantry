using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.PantryItems;

public class UpdateDespensaDto
{
    public DateTime? FechaVencimiento { get; set; }

    [Required]
    public string? CantidadProducto { get; set; }

   // [Required]
    //public string? Unidad { get; set; }
}