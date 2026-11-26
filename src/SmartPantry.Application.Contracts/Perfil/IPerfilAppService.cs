using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Perfil;

public interface IPerfilAppService : IApplicationService
{
    Task<PerfilDto> GetAsync();
    Task<PerfilDto> ActualizarPerfilAsync(ActualizarPerfilDto input);
    Task<string?> ActualizarFotoAsync(ActualizarFotoDto input);
    Task EliminarFotoAsync();
}
