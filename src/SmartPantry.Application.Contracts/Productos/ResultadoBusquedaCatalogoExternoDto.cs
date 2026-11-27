using System;
using System.Collections.Generic;

namespace SmartPantry.Productos;

public class ResultadoBusquedaCatalogoExternoDto
{
    public IReadOnlyList<ExternalProductDto> Items { get; set; } = Array.Empty<ExternalProductDto>();
    public long TotalResultados { get; set; }
    public int Pagina { get; set; }
    public int TamanioPagina { get; set; }
    public int TotalPaginas { get; set; }
}
