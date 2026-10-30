using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
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
            NombreEs = "Arroz Largo Fino",
            NombreEn = "Long Grain Rice",
            Cantidad = "1 kg"
        };

        var result = await _productoAppService.CreateAsync(input);
        var fetched = await _productoAppService.GetAsync(result.Id);

        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(result.Id);
        fetched.NombreEs.ShouldBe("Arroz Largo Fino");
        fetched.NombreEn.ShouldBe("Long Grain Rice");
        fetched.Cantidad.ShouldBe("1 kg");
    }

    [Fact]
    public async Task Should_Not_Create_Producto_With_Missing_Both_Names()
    {
        var input = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791234567890",
            NombreEs = "",
            NombreEn = ""
        };

        await Should.ThrowAsync<AbpValidationException>(() => _productoAppService.CreateAsync(input));
    }

    [Fact]
    public async Task Should_Execute_Full_Crud_Circuit_For_Producto()
    {
        // 1. REGISTRAR
        var createInput = new CreateUpdateProductoDto
        {
            CodigoBarras = "7799999999999",
            NombreEs = "Fideos Tallarines",
            Cantidad = "500 g"
        };
        var creado = await _productoAppService.CreateAsync(createInput);
        creado.Id.ShouldNotBe(Guid.Empty);

        // 2. LISTAR PAGINADO
        var lista = await _productoAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            MaxResultCount = 10,
            SkipCount = 0
        });
        lista.TotalCount.ShouldBeGreaterThan(0);
        lista.Items.ShouldContain(p => p.Id == creado.Id);

        // 3. MODIFICAR
        var updateInput = new CreateUpdateProductoDto
        {
            CodigoBarras = "7799999999999",
            NombreEs = "Fideos Tallarines",
            NombreEn = "Noodles",
            Cantidad = "500 g"
        };
        var modificado = await _productoAppService.UpdateAsync(creado.Id, updateInput);
        modificado.NombreEs.ShouldBe("Fideos Tallarines");
        modificado.NombreEn.ShouldBe("Noodles");

        // 4. CONSULTAR POR ID
        var consultado = await _productoAppService.GetAsync(creado.Id);
        consultado.ShouldNotBeNull();
        consultado.NombreEs.ShouldBe("Fideos Tallarines");
        consultado.NombreEn.ShouldBe("Noodles");

        // 5. ELIMINAR
        await _productoAppService.DeleteAsync(creado.Id);

        // Verificar eliminación en el listado
        var listaPostEliminacion = await _productoAppService.GetListAsync(new PagedAndSortedResultRequestDto());
        listaPostEliminacion.Items.ShouldNotContain(p => p.Id == creado.Id);
    }
}
