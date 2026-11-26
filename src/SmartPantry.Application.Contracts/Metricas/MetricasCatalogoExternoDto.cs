using System;
using System.Collections.Generic;
using SmartPantry.CatalogoExterno;

namespace SmartPantry.Metricas;

public class MetricasCatalogoExternoDto
{
    public DateTime FechaDesdeUtc { get; set; }
    public DateTime FechaHastaUtc { get; set; }

    public int TotalLlamadas { get; set; }
    public int LlamadasExitosas { get; set; }
    public int LlamadasNoEncontradas { get; set; }
    public int LlamadasRateLimited { get; set; }
    public int LlamadasTimeout { get; set; }
    public int LlamadasError { get; set; }

    public double PorcentajeExito { get; set; }
    public double PorcentajeError { get; set; }

    public double LatenciaPromedioMs { get; set; }
    public long LatenciaP95Ms { get; set; }
    public long LatenciaMaximaMs { get; set; }

    public List<MetricasPorOperacionDto> PorOperacion { get; set; } = new();
    public List<MetricasPorDiaDto> SerieDiaria { get; set; } = new();
}

public class MetricasPorOperacionDto
{
    public TipoOperacionCatalogoExterno TipoOperacion { get; set; }
    public int TotalLlamadas { get; set; }
    public int Exitosas { get; set; }
    public int NoEncontradas { get; set; }
    public int Fallidas { get; set; }
    public double PorcentajeExito { get; set; }
    public double PorcentajeError { get; set; }
    public double LatenciaPromedioMs { get; set; }
}

public class MetricasPorDiaDto
{
    public DateTime FechaUtc { get; set; }
    public int TotalLlamadas { get; set; }
    public int Exitosas { get; set; }
    public int Fallidas { get; set; }
    public double LatenciaPromedioMs { get; set; }
}