using Microsoft.AspNetCore.Authorization;
using SmartPantry.Despensa;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.Advertencias;

[Authorize]
public class AdvertenciaVencimientoAppService : ApplicationService, IAdvertenciaVencimientoAppService
{
    private readonly IRepository<AdvertenciaVencimiento, Guid> _advertenciaRepository;

    public AdvertenciaVencimientoAppService(
        IRepository<AdvertenciaVencimiento, Guid> advertenciaRepository)
    {
        _advertenciaRepository = advertenciaRepository;
    }

    public async Task<ListResultDto<AdvertenciaVencimientoDto>> GetActivasAsync()
    {
        var currentUserId = CurrentUser.GetId();

        var queryable = await _advertenciaRepository.GetQueryableAsync();
        var query = queryable
            .Where(x => x.UsuarioId == currentUserId && x.EsActiva)
            .OrderBy(x => x.FechaVencimiento);

        var entidades = await AsyncExecuter.ToListAsync(query);

        var dtos = entidades.Select(x => new AdvertenciaVencimientoDto
        {
            Id = x.Id,
            UsuarioId = x.UsuarioId,
            ItemDespensaId = x.ItemDespensaId,
            TipoAdvertencia = x.TipoAdvertencia,
            EsActiva = x.EsActiva,
            FechaVencimiento = x.FechaVencimiento,
            EstadoVencimiento = CalcularEstadoVencimiento(x.FechaVencimiento)
        }).ToList();

        return new ListResultDto<AdvertenciaVencimientoDto>(dtos);
    }

    public async Task MarcarComoVistaAsync(Guid id)
    {
        var advertencia = await GetAdvertenciaDelUsuarioOThrowAsync(id);

        if (!advertencia.EsActiva)
        {
            return;
        }

        advertencia.Desactivar();
        await _advertenciaRepository.UpdateAsync(advertencia, autoSave: true);
    }

    private async Task<AdvertenciaVencimiento> GetAdvertenciaDelUsuarioOThrowAsync(Guid id)
    {
        var advertencia = await _advertenciaRepository.FindAsync(id);

        if (advertencia == null || advertencia.UsuarioId != CurrentUser.GetId())
        {
            throw new EntityNotFoundException(typeof(AdvertenciaVencimiento), id);
        }

        return advertencia;
    }

    private EstadoVencimiento CalcularEstadoVencimiento(DateTime? fechaVencimiento)
    {
        if (!fechaVencimiento.HasValue)
        {
            return EstadoVencimiento.SinFecha;
        }

        var hoy = Clock.Now.Date;
        var vencimiento = fechaVencimiento.Value.Date;
        var diasRestantes = (vencimiento - hoy).Days;

        if (diasRestantes < 0)
        {
            return EstadoVencimiento.Vencido;
        }

        if (diasRestantes <= UmbralVencimiento.DiasPorDefecto)
        {
            return EstadoVencimiento.ProximoAVencer;
        }

        return EstadoVencimiento.Vigente;
    }
}