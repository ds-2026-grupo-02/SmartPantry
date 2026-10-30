using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.PantryItems;

public class CreateDespensaDto
{
    [Required]
    public Guid ProductoId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
    public decimal? Cantidad { get; set; }

    [StringLength(64)]
    public string? Unidad { get; set; }

    public DateTime? FechaVencimiento { get; set; }
}
