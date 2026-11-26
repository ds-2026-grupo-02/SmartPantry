using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Preferencias;

public interface IPreferenciaAppService : IApplicationService
{
    Task<PreferenciaDto> GetAsync();
    Task<PreferenciaDto> UpdateAsync(UpdatePreferenciaDto input);
}
