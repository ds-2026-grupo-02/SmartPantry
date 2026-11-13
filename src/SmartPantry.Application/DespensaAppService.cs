using Microsoft.AspNetCore.Authorization;
using SmartPantry.Productos;
using System;
using System.Linq;
using System.Threading.Tasks;
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

        var dtos = items.Select(x => ObjectMapper.Map<PantryItem, DespensaDto>(x)).ToList();

        // Completar los datos del producto asociado
        foreach (var dto in dtos)
        {
            var producto = await _productoRepository.FindAsync(dto.ProductoId);
            if (producto != null)
            {
                dto.NombreProductoEs = producto.NombreEs;
                dto.NombreProductoEn = producto.NombreEn;
                dto.CantidadProducto = producto.Cantidad;
            }
        }

        return new PagedResultDto<DespensaDto>(totalCount, dtos);
    }

    public async Task<DespensaDto> CreateAsync(CreateDespensaDto input)
    {
        var currentUserId = CurrentUser.GetId();

        // Validar que el producto exista (lanza EntityNotFoundException/404 si no existe)
        var producto = await _productoRepository.GetAsync(input.ProductoId);

        var item = new PantryItem(
            GuidGenerator.Create(),
            currentUserId,
            input.ProductoId,
            input.FechaVencimiento
        );

        await _pantryItemRepository.InsertAsync(item);

        var dto = ObjectMapper.Map<PantryItem, DespensaDto>(item);
        dto.NombreProductoEs = producto.NombreEs;
        dto.NombreProductoEn = producto.NombreEn;
        dto.CantidadProducto = producto.Cantidad;
        return dto;
    }

    public async Task<DespensaDto> UpdateAsync(Guid id, UpdateDespensaDto input)
    {
        var item = await GetItemDelUsuarioOThrowAsync(id);

        item.UpdateExpirationDate(input.FechaVencimiento);

        await _pantryItemRepository.UpdateAsync(item);

        var dto = ObjectMapper.Map<PantryItem, DespensaDto>(item);
        var producto = await _productoRepository.FindAsync(dto.ProductoId);
        if (producto != null)
        {
            dto.NombreProductoEs = producto.NombreEs;
            dto.NombreProductoEn = producto.NombreEn;
            dto.CantidadProducto = producto.Cantidad;
        }
        return dto;
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
