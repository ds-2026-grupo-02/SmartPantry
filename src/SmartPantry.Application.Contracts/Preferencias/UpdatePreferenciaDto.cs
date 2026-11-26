using System.Collections.Generic;

namespace SmartPantry.Preferencias;

public class UpdatePreferenciaDto
{
    public List<string> Alergenos { get; set; } = new();
    public List<string> Ingredientes { get; set; } = new();
    public bool MostrarAlertas { get; set; }
}
