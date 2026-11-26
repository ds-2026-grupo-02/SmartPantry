using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.CatalogoExterno;
using SmartPantry.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;

namespace SmartPantry.Metricas;

[Authorize(SmartPantryPermissions.Metricas.VerCatalogoExterno)]
public class MetricasCatalogoExternoAppService : ApplicationService, IMetricasCatalogoExternoAppService
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
        var ahoraUtc = _clock.Now.ToUniversalTime();
        var fechaHasta = (input.FechaHastaUtc ?? ahoraUtc).ToUniversalTime();
        var fechaDesde = (input.FechaDesdeUtc ?? fechaHasta.AddDays(-7)).ToUniversalTime();

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

        var registros = await AsyncExecuter.ToListAsync(filtro);

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

        var serieDiaria = registros
            .GroupBy(r => r.FechaHoraUtc.Date)
            .OrderBy(g => g.Key)
            .Select(g => new MetricasPorDiaDto
            {
                FechaUtc = DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                TotalLlamadas = g.Count(),
                Exitosas = g.Count(x => x.Resultado == ResultadoLlamadaExterna.Success),
                Fallidas = g.Count(x => x.Resultado == ResultadoLlamadaExterna.RateLimited ||
                                        x.Resultado == ResultadoLlamadaExterna.Timeout ||
                                        x.Resultado == ResultadoLlamadaExterna.Error),
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
            SerieDiaria = serieDiaria
        };
    }
}