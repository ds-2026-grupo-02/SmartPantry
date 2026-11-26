using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Advertencias;

public interface IAdvertenciaVencimientoAppService : IApplicationService
{
    Task<ListResultDto<AdvertenciaVencimientoDto>> GetActivasAsync();
    Task MarcarComoVistaAsync(Guid id);
}