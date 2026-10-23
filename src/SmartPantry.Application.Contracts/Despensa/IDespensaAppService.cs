using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.PantryItems;

public interface IDespensaAppService : IApplicationService
{
    Task<PagedResultDto<DespensaDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<DespensaDto> UpdateAsync(Guid id, UpdateDespensaDto input);
    Task MarcarConsumidoAsync(Guid id);
    Task DeleteAsync(Guid id);
}
