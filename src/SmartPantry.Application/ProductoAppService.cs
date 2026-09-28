using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using SmartPantry.Productos;

namespace SmartPantry.Productos;

public class ProductoAppService : 
    CrudAppService<
        Producto, 
        ProductoDto, 
        Guid, 
        PagedAndSortedResultRequestDto, 
        CreateUpdateProductoDto>,
    IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;

    public ProductoAppService(IRepository<Producto, Guid> repository)
    : base(repository)
         { _productoRepository = repository; }

public override async Task<ProductoDto> CreateAsync(CreateUpdateProductoDto input)
    {
        // 1. Instanciar el Agregado (Aplica internamente las validaciones del dominio)
        var producto = new Producto(
            GuidGenerator.Create(),
            input.Nombre,
            input.CodigoBarras
        );

        // 2. Persistir a través del repositorio
        await _productoRepository.InsertAsync(producto);

        // 3. Mapear y retornar DTO
        return MapToGetOutputDto(producto);
    }

    public override async Task<ProductoDto> UpdateAsync(Guid id, CreateUpdateProductoDto input)
    {
        // Obtiene la entidad por Id (Si no existe, ABP lanza EntityNotFoundException/404 de forma predeterminada)
        var producto = await _productoRepository.GetAsync(id);
        producto.ModificarDatos(input.Nombre, input.CodigoBarras);
        await _productoRepository.UpdateAsync(producto);

    return MapToGetOutputDto(producto);
    }
}