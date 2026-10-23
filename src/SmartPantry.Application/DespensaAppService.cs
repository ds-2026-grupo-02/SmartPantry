using Microsoft.AspNetCore.Authorization;
using SmartPantry.Productos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.PantryItems;

[Authorize]
public class DespensaAppService : ApplicationService, IDespensaAppService
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<Producto, Guid> _productoRepository;

    public DespensaAppService(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IRepository<Producto, Guid> productoRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _productoRepository = productoRepository;
    }

    public async Task<PagedResultDto<DespensaDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var currentUserId = CurrentUser.GetId();

        var queryable = await _pantryItemRepository.GetQueryableAsync();
        var userQuery = queryable.Where(x => x.UserId == currentUserId && !x.EsConsumido);

        var totalCount = await AsyncExecuter.CountAsync(userQuery);

        var items = await AsyncExecuter.ToListAsync(
            userQuery.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        var dtos = ObjectMapper.Map<List<PantryItem>, List<DespensaDto>>(items);

        // Completar el nombre del producto si aplica
        foreach (var dto in dtos)
        {
            var producto = await _productoRepository.FindAsync(dto.ProductoId);
            if (producto != null)
            {
                dto.NombreProducto = producto.Nombre;
            }
        }

        return new PagedResultDto<DespensaDto>(totalCount, dtos);
    }

    public async Task<DespensaDto> UpdateAsync(Guid id, UpdateDespensaDto input)
    {
        var item = await GetItemDelUsuarioOThrowAsync(id);

        item.SetCantidadYUnidad(input.Cantidad, input.Unidad);
        item.UpdateExpirationDate(input.FechaVencimiento);

        await _pantryItemRepository.UpdateAsync(item);
        return ObjectMapper.Map<PantryItem, DespensaDto>(item);
    }

    public async Task MarcarConsumidoAsync(Guid id)
    {
        var item = await GetItemDelUsuarioOThrowAsync(id);
        item.MarcarComoConsumido();
        await _pantryItemRepository.UpdateAsync(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetItemDelUsuarioOThrowAsync(id);
        await _pantryItemRepository.DeleteAsync(item);
    }

    private async Task<PantryItem> GetItemDelUsuarioOThrowAsync(Guid id)
    {
        var item = await _pantryItemRepository.FindAsync(id);

        if (item == null || item.UserId != CurrentUser.GetId())
        {
            throw new EntityNotFoundException(typeof(PantryItem), id);
        }

        return item;
    }
}