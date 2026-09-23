using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;
using SmartPantry.Productos;

namespace SmartPantry.EntityFrameworkCore.Applications;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class ProductoAppServiceTests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IProductoAppService _productoAppService;

    public ProductoAppServiceTests()
    {
        _productoAppService = GetRequiredService<IProductoAppService>();
    }

    [Fact]
    public async Task Should_Create_And_Get_Producto_By_Id()
    {
        var input = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Arroz Largo Fino"
        };

        var result = await _productoAppService.CreateAsync(input);
        var fetched = await _productoAppService.GetAsync(result.Id);

        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(result.Id);
        fetched.Nombre.ShouldBe("Arroz Largo Fino");
    }

    [Fact]
    public async Task Should_Not_Create_Producto_With_Missing_Required_Fields()
    {
        var input = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = ""
        };

        await Should.ThrowAsync<AbpValidationException>(() => _productoAppService.CreateAsync(input));
    }
}
