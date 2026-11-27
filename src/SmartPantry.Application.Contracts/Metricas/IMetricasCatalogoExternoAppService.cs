using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Metricas;

public interface IMetricasCatalogoExternoAppService : IApplicationService
{
    Task<MetricasCatalogoExternoDto> GetResumenAsync(GetMetricasCatalogoExternoInput input);
}