using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using SmartPantry.Productos;
using SmartPantry.PantryItems;

namespace SmartPantry;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SmartPantryApplicationMappers : MapperBase<Producto, ProductoDto>
{
    public override partial ProductoDto Map(Producto source);

    public override partial void Map(Producto source, ProductoDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class DespensaApplicationMappers : MapperBase<PantryItem, DespensaDto>
{
    public override partial DespensaDto Map(PantryItem source);

    public override partial void Map(PantryItem source, DespensaDto destination);
}
