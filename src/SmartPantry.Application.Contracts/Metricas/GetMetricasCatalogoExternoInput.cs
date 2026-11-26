using System;
using SmartPantry.CatalogoExterno;

namespace SmartPantry.Metricas;

public class GetMetricasCatalogoExternoInput
{
    public DateTime? FechaDesdeUtc { get; set; }
    public DateTime? FechaHastaUtc { get; set; }
    public TipoOperacionCatalogoExterno? TipoOperacion { get; set; }
}