using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using SmartPantry.Productos;

namespace SmartPantry.Productos;

public class ProductoAppService : ApplicationService, IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;

    public ProductoAppService(IRepository<Producto, Guid> productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task<ProductoDto> CreateAsync(CreateUpdateProductoDto input)
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
        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }

    public async Task<ProductoDto> GetAsync(Guid id)
    {
        // Obtiene la entidad por Id (Si no existe, ABP lanza EntityNotFoundException/404 de forma predeterminada)
        var producto = await _productoRepository.GetAsync(id);

        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }
}