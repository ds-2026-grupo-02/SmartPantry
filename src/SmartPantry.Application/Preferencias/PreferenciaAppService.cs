using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.Preferencias;

[Authorize]
public class PreferenciaAppService : ApplicationService, IPreferenciaAppService
{
    private readonly IRepository<PreferenciaUsuario, Guid> _preferenciaRepository;

    public PreferenciaAppService(IRepository<PreferenciaUsuario, Guid> preferenciaRepository)
    {
        _preferenciaRepository = preferenciaRepository;
    }

    public async Task<PreferenciaDto> GetAsync()
    {
        var currentUserId = CurrentUser.GetId();

        var queryable = await _preferenciaRepository.GetQueryableAsync();
        var preferencia = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(p => p.UsuarioId == currentUserId));

        if (preferencia == null)
        {
            return new PreferenciaDto
            {
                Alergenos = new(),
                Ingredientes = new(),
                MostrarAlertas = true
            };
        }

        return MapToDto(preferencia);
    }

    public async Task<PreferenciaDto> UpdateAsync(UpdatePreferenciaDto input)
    {
        var currentUserId = CurrentUser.GetId();

        var queryable = await _preferenciaRepository.GetQueryableAsync();
        var preferencia = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(p => p.UsuarioId == currentUserId));

        if (preferencia == null)
        {
            preferencia = new PreferenciaUsuario(GuidGenerator.Create(), currentUserId);
            preferencia.Actualizar(
                input.Alergenos ?? new(),
                input.Ingredientes ?? new(),
                input.MostrarAlertas);
            await _preferenciaRepository.InsertAsync(preferencia, autoSave: true);
        }
        else
        {
            preferencia.Actualizar(
                input.Alergenos ?? new(),
                input.Ingredientes ?? new(),
                input.MostrarAlertas);
            await _preferenciaRepository.UpdateAsync(preferencia);
        }

        return MapToDto(preferencia);
    }

    private static PreferenciaDto MapToDto(PreferenciaUsuario preferencia)
    {
        return new PreferenciaDto
        {
            Alergenos = preferencia.Alergenos.Select(a => a.Nombre).ToList(),
            Ingredientes = preferencia.Ingredientes.Select(i => i.Nombre).ToList(),
            MostrarAlertas = preferencia.MostrarAlertas
        };
    }
}
