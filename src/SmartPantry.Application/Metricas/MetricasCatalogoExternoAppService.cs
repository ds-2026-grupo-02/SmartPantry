using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.CatalogoExterno;
using SmartPantry.Permissions;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;

namespace SmartPantry.Metricas;

[Authorize(SmartPantryPermissions.Metricas.VerCatalogoExterno)]
public class MetricasCatalogoExternoAppService : SmartPantryAppService, IMetricasCatalogoExternoAppService
{
    private readonly IRepository<RegistroLlamadaExterna, Guid> _registroRepository;
    private readonly IClock _clock;

    public MetricasCatalogoExternoAppService(
        IRepository<RegistroLlamadaExterna, Guid> registroRepository,
        IClock clock)
    {
        _registroRepository = registroRepository;
        _clock = clock;
    }

    public async Task<MetricasCatalogoExternoDto> GetResumenAsync(GetMetricasCatalogoExternoInput input)
    {
        input ??= new GetMetricasCatalogoExternoInput();

        var ahoraUtc = NormalizarComoUtc(_clock.Now);
        var fechaHasta = input.FechaHastaUtc.HasValue
            ? NormalizarComoUtc(input.FechaHastaUtc.Value)
            : ahoraUtc;
        var fechaDesde = input.FechaDesdeUtc.HasValue
            ? NormalizarComoUtc(input.FechaDesdeUtc.Value)
            : fechaHasta.AddDays(-7);

        if (fechaDesde > fechaHasta)
        {
            throw new UserFriendlyException("La fecha de inicio no puede ser posterior a la fecha de fin.");
        }

        var queryable = await _registroRepository.GetQueryableAsync();

        var filtro = queryable
            .Where(r => r.FechaHoraUtc >= fechaDesde && r.FechaHoraUtc <= fechaHasta);

        if (input.TipoOperacion.HasValue)
        {
            filtro = filtro.Where(r => r.TipoOperacion == input.TipoOperacion.Value);
        }

        // Proyectar a tipo anónimo en EF Core omite automáticamente el Change Tracking
        var registros = await AsyncExecuter.ToListAsync(
            filtro.Select(r => new
            {
                r.FechaHoraUtc,
                r.Resultado,
                r.DuracionMs,
                r.TipoOperacion
            })
        );

        var total = registros.Count;
        if (total == 0)
        {
            return new MetricasCatalogoExternoDto
            {
                FechaDesdeUtc = fechaDesde,
                FechaHastaUtc = fechaHasta
            };
        }

        var exitosas = registros.Count(r => r.Resultado == ResultadoLlamadaExterna.Success);
        var noEncontradas = registros.Count(r => r.Resultado == ResultadoLlamadaExterna.NotFound);
        var rateLimited = registros.Count(r => r.Resultado == ResultadoLlamadaExterna.RateLimited);
        var timeout = registros.Count(r => r.Resultado == ResultadoLlamadaExterna.Timeout);
        var errores = registros.Count(r => r.Resultado == ResultadoLlamadaExterna.Error);

        var fallidasReales = rateLimited + timeout + errores;

        var duracionesOrdenadas = registros
            .Select(r => r.DuracionMs)
            .OrderBy(d => d)
            .ToList();

        var p95Index = (int)Math.Ceiling(duracionesOrdenadas.Count * 0.95) - 1;
        var latenciaP95 = duracionesOrdenadas[Math.Max(0, p95Index)];

        var porOperacion = registros
            .GroupBy(r => r.TipoOperacion)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var totalOp = g.Count();
                var exitosasOp = g.Count(x => x.Resultado == ResultadoLlamadaExterna.Success);
                var noEncontradasOp = g.Count(x => x.Resultado == ResultadoLlamadaExterna.NotFound);
                var fallidasOp = g.Count(x => EsFalloReal(x.Resultado));

                return new MetricasPorOperacionDto
                {
                    TipoOperacion = g.Key,
                    TotalLlamadas = totalOp,
                    Exitosas = exitosasOp,
                    NoEncontradas = noEncontradasOp,
                    Fallidas = fallidasOp,
                    PorcentajeExito = Math.Round((double)exitosasOp * 100 / totalOp, 2),
                    PorcentajeError = Math.Round((double)fallidasOp * 100 / totalOp, 2),
                    LatenciaPromedioMs = Math.Round(g.Average(x => x.DuracionMs), 2)
                };
            })
            .ToList();

        var serieDiaria = registros
            .GroupBy(r => r.FechaHoraUtc.Date)
            .OrderBy(g => g.Key)
            .Select(g => new MetricasPorDiaDto
            {
                FechaUtc = DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                TotalLlamadas = g.Count(),
                Exitosas = g.Count(x => x.Resultado == ResultadoLlamadaExterna.Success),
                Fallidas = g.Count(x => EsFalloReal(x.Resultado)),
                LatenciaPromedioMs = Math.Round(g.Average(x => x.DuracionMs), 2)
            })
            .ToList();

        return new MetricasCatalogoExternoDto
        {
            FechaDesdeUtc = fechaDesde,
            FechaHastaUtc = fechaHasta,
            TotalLlamadas = total,
            LlamadasExitosas = exitosas,
            LlamadasNoEncontradas = noEncontradas,
            LlamadasRateLimited = rateLimited,
            LlamadasTimeout = timeout,
            LlamadasError = errores,
            PorcentajeExito = Math.Round((double)exitosas * 100 / total, 2),
            PorcentajeError = Math.Round((double)fallidasReales * 100 / total, 2),
            LatenciaPromedioMs = Math.Round(registros.Average(r => r.DuracionMs), 2),
            LatenciaP95Ms = latenciaP95,
            LatenciaMaximaMs = duracionesOrdenadas[^1],
            PorOperacion = porOperacion,
            SerieDiaria = serieDiaria
        };
    }

    private static bool EsFalloReal(ResultadoLlamadaExterna resultado)
    {
        return resultado is ResultadoLlamadaExterna.RateLimited
            or ResultadoLlamadaExterna.Timeout
            or ResultadoLlamadaExterna.Error;
    }

    private static DateTime NormalizarComoUtc(DateTime fecha)
    {
        return fecha.Kind switch
        {
            DateTimeKind.Utc => fecha,
            DateTimeKind.Local => fecha.ToUniversalTime(),
            _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
        };
    }
}