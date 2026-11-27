using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.CatalogoExterno;

namespace SmartPantry.Productos;

[Authorize]
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
    private readonly IExternalProductCatalogClient _externalProductCatalogClient;

    public ProductoAppService(
        IRepository<Producto, Guid> repository,
        IExternalProductCatalogClient externalProductCatalogClient)
        : base(repository)
    {
        _productoRepository = repository;
        _externalProductCatalogClient = externalProductCatalogClient;
    }

    public override async Task<ProductoDto> CreateAsync(CreateUpdateProductoDto input)
    {
        var codigoBarras = input.CodigoBarras.Trim();

        if (await _productoRepository.AnyAsync(p => p.CodigoBarras == codigoBarras))
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.ProductoCodigoBarrasDuplicado)
                .WithData("CodigoBarras", codigoBarras);
        }

        // 1. Instanciar el Agregado (Aplica internamente las validaciones del dominio)
        var producto = new Producto(
            GuidGenerator.Create(),
            input.NombreEs,
            input.NombreEn,
            codigoBarras,
            input.Cantidad,
            input.Marca,
            input.ImagenUrl,
            input.Categorias,
            input.Ingredientes,
            input.Alergenos,
            input.NutriScore,
            input.Nova
        );

        // 2. Persistir a través del repositorio
        await _productoRepository.InsertAsync(producto, autoSave: true);

        // 3. Mapear y retornar DTO
        return MapToGetOutputDto(producto);
    }

    public override async Task<ProductoDto> UpdateAsync(Guid id, CreateUpdateProductoDto input)
    {
        // Obtiene la entidad por Id (Si no existe, ABP lanza EntityNotFoundException/404 de forma predeterminada)
        var producto = await _productoRepository.GetAsync(id);
        var codigoBarras = input.CodigoBarras.Trim();

        if (await _productoRepository.AnyAsync(p => p.CodigoBarras == codigoBarras && p.Id != id))
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.ProductoCodigoBarrasDuplicado)
                .WithData("CodigoBarras", codigoBarras);
        }

        producto.ModificarDatos(
            input.NombreEs,
            input.NombreEn,
            codigoBarras,
            input.Cantidad,
            input.Marca,
            input.ImagenUrl,
            input.Categorias,
            input.Ingredientes,
            input.Alergenos,
            input.NutriScore,
            input.Nova
        );

        await _productoRepository.UpdateAsync(producto, autoSave: true);

        return MapToGetOutputDto(producto);
    }

    public async Task<ExternalProductDto?> SearchByBarcodeAsync(SearchProductInputDto input)
    {
        // 1. Validar el parámetro de entrada
        if (input == null || string.IsNullOrWhiteSpace(input.CodigoBarras))
        {
            throw new UserFriendlyException("El código de barras es requerido para realizar la búsqueda.");
        }

        // 2. Consultar el cliente externo (Open Food Facts API v3)
        var externalProduct = await EjecutarConsultaCatalogoExternoAsync(
            () => _externalProductCatalogClient.GetByBarcodeAsync(input.CodigoBarras.Trim()));

        // 3. Manejo de producto no encontrado (RF-05 / RF-09)
        if (externalProduct == null)
        {
            throw new UserFriendlyException($"No se encontró ningún producto con el código de barras '{input.CodigoBarras}'.");
        }

        return externalProduct;
    }

    public async Task<ResultadoBusquedaCatalogoExternoDto> SearchByTextAsync(BuscarProductoPorTextoInputDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Texto))
        {
            throw new UserFriendlyException("El texto de búsqueda es requerido.");
        }

        var texto = input.Texto.Trim();
        if (texto.Length > 200)
        {
            throw new UserFriendlyException("El texto de búsqueda no puede superar los 200 caracteres.");
        }

        if (input.Pagina < 1)
        {
            throw new UserFriendlyException("La página debe ser mayor o igual a 1.");
        }

        if (input.TamanioPagina < 1 || input.TamanioPagina > BuscarProductoPorTextoInputDto.TamanioPaginaMaximo)
        {
            throw new UserFriendlyException($"El tamaño de página debe estar entre 1 y {BuscarProductoPorTextoInputDto.TamanioPaginaMaximo}.");
        }

        if ((long)input.Pagina * input.TamanioPagina > BuscarProductoPorTextoInputDto.VentanaMaximaResultados)
        {
            throw new UserFriendlyException($"La paginación excede el límite máximo permitido ({BuscarProductoPorTextoInputDto.VentanaMaximaResultados} resultados).");
        }

        IReadOnlyList<string>? idiomas = input.Idiomas?
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (idiomas != null && idiomas.Count == 0)
        {
            idiomas = null;
        }

        var resultado = await EjecutarConsultaCatalogoExternoAsync(
            () => _externalProductCatalogClient.SearchByTextAsync(
                texto,
                input.Pagina,
                input.TamanioPagina,
                idiomas));

        return resultado ?? new ResultadoBusquedaCatalogoExternoDto
        {
            Items = Array.Empty<ExternalProductDto>(),
            TotalResultados = 0,
            Pagina = input.Pagina,
            TamanioPagina = input.TamanioPagina,
            TotalPaginas = 0
        };
    }

    public async Task<ProductoDto> SaveFromExternalAsync(SearchProductInputDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.CodigoBarras))
        {
            throw new UserFriendlyException("El código de barras es requerido para realizar la búsqueda.");
        }

        var codigoBarras = input.CodigoBarras.Trim();

        // 1. Si el producto ya existe en BD, devolverlo sin volver a consultar la API externa
        var existente = await _productoRepository.FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras);
        if (existente != null)
        {
            return MapToGetOutputDto(existente);
        }

        // 2. Consultar el catálogo externo (Open Food Facts)
        var externalProduct = await EjecutarConsultaCatalogoExternoAsync(
            () => _externalProductCatalogClient.GetByBarcodeAsync(codigoBarras));
        if (externalProduct == null)
        {
            throw new UserFriendlyException($"No se encontró ningún producto con el código de barras '{codigoBarras}'.");
        }

        if (string.IsNullOrWhiteSpace(externalProduct.NombreEs) && string.IsNullOrWhiteSpace(externalProduct.Nombre))
        {
            throw new UserFriendlyException($"El producto con el código de barras '{codigoBarras}' no tiene un nombre válido en el catálogo externo.");
        }

        // 3. Crear y persistir preservando null en los campos faltantes
        var producto = new Producto(
            GuidGenerator.Create(),
            externalProduct.NombreEs,
            externalProduct.Nombre,
            codigoBarras,
            externalProduct.Cantidad,
            externalProduct.Marca,
            externalProduct.ImagenUrl,
            externalProduct.Categorias,
            externalProduct.Ingredientes,
            externalProduct.Alergenos,
            externalProduct.NutriScore,
            externalProduct.Nova
        );

        await _productoRepository.InsertAsync(producto, autoSave: true);

        return MapToGetOutputDto(producto);
    }

    private static async Task<T> EjecutarConsultaCatalogoExternoAsync<T>(Func<Task<T>> accion)
    {
        try
        {
            return await accion();
        }
        catch (CatalogoExternoException ex)
        {
            throw TraducirErrorCatalogoExterno(ex);
        }
    }

    private static UserFriendlyException TraducirErrorCatalogoExterno(CatalogoExternoException ex)
    {
        return ex.TipoError switch
        {
            TipoErrorCatalogoExterno.RateLimited => new UserFriendlyException(
                "El catálogo externo limitó temporalmente las consultas. Por favor, intente nuevamente más tarde.",
                code: SmartPantryDomainErrorCodes.CatalogoExternoRateLimit),

            TipoErrorCatalogoExterno.Timeout => new UserFriendlyException(
                "La consulta al catálogo externo excedió el tiempo de espera. Por favor, intente nuevamente.",
                code: SmartPantryDomainErrorCodes.CatalogoExternoTimeout),

            _ => new UserFriendlyException(
                "El catálogo externo de productos no se encuentra disponible en este momento.",
                code: SmartPantryDomainErrorCodes.CatalogoExternoNoDisponible)
        };
    }
}
