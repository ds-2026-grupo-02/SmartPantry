using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

public interface IProductoAppService : IApplicationService
{
    Task<ProductoDto> CreateAsync(CreateUpdateProductoDto input);
    Task<ProductoDto> GetAsync(Guid id);
}