using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using SmartPantry.Productos;


namespace SmartPantry;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SmartPantryApplicationMappers : MapperBase<Producto, ProductoDto>
{
    public override partial ProductoDto Map(Producto source);

    public override partial void Map(Producto source, ProductoDto destination);
}