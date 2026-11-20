using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace SmartPantry.Despensa;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class DespensaMapper : MapperBase<ItemDespensa, DespensaDto>
{
    [MapperIgnoreTarget(nameof(DespensaDto.NombreProductoEs))]
    [MapperIgnoreTarget(nameof(DespensaDto.NombreProductoEn))]
    [MapperIgnoreTarget(nameof(DespensaDto.CantidadProducto))]
    public override partial DespensaDto Map(ItemDespensa source);

    [MapperIgnoreTarget(nameof(DespensaDto.NombreProductoEs))]
    [MapperIgnoreTarget(nameof(DespensaDto.NombreProductoEn))]
    [MapperIgnoreTarget(nameof(DespensaDto.CantidadProducto))]
    public override partial void Map(ItemDespensa source, DespensaDto destination);
}
