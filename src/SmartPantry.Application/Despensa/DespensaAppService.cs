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

namespace SmartPantry.Despensa;

[Authorize]
public class DespensaAppService : ApplicationService, IDespensaAppService
{
    private readonly IRepository<ItemDespensa, Guid> _itemDespensaRepository;
    private readonly IRepository<Producto, Guid> _productoRepository;
    private readonly IRepository<NotaPrivadaDespensa, Guid> _notaPrivadaRepository;
    public DespensaAppService(
        IRepository<ItemDespensa, Guid> pantryItemRepository,
        IRepository<Producto, Guid> productoRepository,
        IRepository<NotaPrivadaDespensa, Guid> notaPrivadaRepository)
    {
        _itemDespensaRepository = pantryItemRepository;
        _productoRepository = productoRepository;
        _notaPrivadaRepository = notaPrivadaRepository;
    }

    public async Task<PagedResultDto<DespensaDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var currentUserId = CurrentUser.GetId();

        var queryable = await _itemDespensaRepository.GetQueryableAsync();
        var userQuery = queryable.Where(x => x.UsuarioId == currentUserId && !x.EsConsumido);

        var totalCount = await AsyncExecuter.CountAsync(userQuery);

        var items = await AsyncExecuter.ToListAsync(
            userQuery.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        var dtos = items.Select(x => ObjectMapper.Map<ItemDespensa, DespensaDto>(x)).ToList();

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

        var item = new ItemDespensa(
            GuidGenerator.Create(),
            currentUserId,
            input.ProductoId,
            input.FechaVencimiento
        );

        await _itemDespensaRepository.InsertAsync(item);

        var dto = ObjectMapper.Map<ItemDespensa, DespensaDto>(item);
        dto.NombreProductoEs = producto.NombreEs;
        dto.NombreProductoEn = producto.NombreEn;
        dto.CantidadProducto = producto.Cantidad;
        return dto;
    }

    public async Task<DespensaDto> UpdateAsync(Guid id, UpdateDespensaDto input)
    {
        var item = await GetItemDelUsuarioOThrowAsync(id);

        item.ActualizarFechaVencimiento(input.FechaVencimiento);

        await _itemDespensaRepository.UpdateAsync(item);

        var dto = ObjectMapper.Map<ItemDespensa, DespensaDto>(item);
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
        await _itemDespensaRepository.UpdateAsync(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetItemDelUsuarioOThrowAsync(id);
        await _itemDespensaRepository.DeleteAsync(item);
    }
    public async Task<NotaPrivadaDto?> GetNotaPrivadaAsync(Guid itemId)
    {
        await GetItemDelUsuarioOThrowAsync(itemId);
        var currentUserId = CurrentUser.GetId();

        var nota = await _notaPrivadaRepository.FirstOrDefaultAsync(x =>
            x.ItemDespensaId == itemId && x.UsuarioId == currentUserId);

        if (nota == null)
        {
            return null;
        }

        return new NotaPrivadaDto
        {
            Id = nota.Id,
            ItemDespensaId = nota.ItemDespensaId,
            Contenido = nota.Contenido
        };
    }

    public async Task<NotaPrivadaDto> GuardarNotaPrivadaAsync(Guid itemId, GuardarNotaPrivadaDto input)
    {
        await GetItemDelUsuarioOThrowAsync(itemId);
        var currentUserId = CurrentUser.GetId();

        var nota = await _notaPrivadaRepository.FirstOrDefaultAsync(x =>
            x.ItemDespensaId == itemId && x.UsuarioId == currentUserId);

        if (nota == null)
        {
            nota = new NotaPrivadaDespensa(
                GuidGenerator.Create(),
                itemId,
                currentUserId,
                input.Contenido
            );
            await _notaPrivadaRepository.InsertAsync(nota, autoSave: true);
        }
        else
        {
            nota.ActualizarContenido(input.Contenido);
            await _notaPrivadaRepository.UpdateAsync(nota, autoSave: true);
        }

        return new NotaPrivadaDto
        {
            Id = nota.Id,
            ItemDespensaId = nota.ItemDespensaId,
            Contenido = nota.Contenido
        };
    }

    public async Task EliminarNotaPrivadaAsync(Guid itemId)
    {
        await GetItemDelUsuarioOThrowAsync(itemId);
        var currentUserId = CurrentUser.GetId();

        var nota = await _notaPrivadaRepository.FirstOrDefaultAsync(x =>
            x.ItemDespensaId == itemId && x.UsuarioId == currentUserId);

        if (nota != null)
        {
            await _notaPrivadaRepository.DeleteAsync(nota, autoSave: true);
        }
    }

    private async Task<ItemDespensa> GetItemDelUsuarioOThrowAsync(Guid id)
    {
        var item = await _itemDespensaRepository.FindAsync(id);

        if (item == null || item.UsuarioId != CurrentUser.GetId())
        {
            throw new EntityNotFoundException(typeof(ItemDespensa), id);
        }

        return item;
    }
}
