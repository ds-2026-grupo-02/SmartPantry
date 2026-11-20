using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace SmartPantry.Productos;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ProductoMapper : MapperBase<Producto, ProductoDto>
{
    public override partial ProductoDto Map(Producto source);

    public override partial void Map(Producto source, ProductoDto destination);
}
