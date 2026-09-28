using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

public interface IProductoAppService :
    ICrudAppService< // Defines CRUD methods
        ProductoDto, // Used to show products
        Guid, // Primary key of the product entity
        PagedAndSortedResultRequestDto, // Used for paging/sorting
        CreateUpdateProductoDto> // Used to create/update a product>
{ }