using System.Collections.Generic;

namespace SmartPantry.Preferencias;

public class PreferenciaDto
{
    public List<string> Alergenos { get; set; } = new();
    public List<string> Ingredientes { get; set; } = new();
    public bool MostrarAlertas { get; set; }
}
