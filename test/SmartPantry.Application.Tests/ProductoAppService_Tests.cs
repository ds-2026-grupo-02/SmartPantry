using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;
using SmartPantry;

namespace SmartPantry.Productos;

public class ProductoAppService_Tests : SmartPantryApplicationTestBase<SmartPantryApplicationTestModule>
{
    private readonly IProductoAppService _productoAppService;

    public ProductoAppService_Tests()
    {
        // Resolver el servicio de aplicación desde el contenedor DI de ABP
        _productoAppService = GetRequiredService<IProductoAppService>();
    }

    [Fact]
    public async Task Should_Create_And_Get_Producto_By_Id()
    {
        // Arrange
        var input = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Arroz Largo Fino"
        };

        // Act
        var result = await _productoAppService.CreateAsync(input);
        var fetched = await _productoAppService.GetAsync(result.Id);

        // Assert
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(result.Id);
        fetched.Nombre.ShouldBe("Arroz Largo Fino");
    }

    [Fact]
    public async Task Should_Not_Create_Producto_With_Missing_Required_Fields()
    {
        // Arrange: DTO sin Nombre (campo obligatorio)
        var input = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = ""
        };

        // Act & Assert: ABP intercepta la validación del DTO
        await Should.ThrowAsync<AbpValidationException>(async () =>
        {
            await _productoAppService.CreateAsync(input);
        });
    }
}