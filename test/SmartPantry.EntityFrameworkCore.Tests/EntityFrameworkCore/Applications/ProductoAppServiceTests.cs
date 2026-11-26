using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using SmartPantry.CatalogoExterno;
using SmartPantry.Productos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Validation;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Applications;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class ProductoAppServiceTests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IProductoAppService _productoAppService;
    private readonly IRepository<Producto, Guid> _productoRepository;
    private readonly IExternalProductCatalogClient _externalProductCatalogClient;

    public ProductoAppServiceTests()
    {
        _productoAppService = GetRequiredService<IProductoAppService>();
        _productoRepository = GetRequiredService<IRepository<Producto, Guid>>();
        _externalProductCatalogClient = GetRequiredService<IExternalProductCatalogClient>();
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

    [Fact]
    public async Task Should_Persist_And_Retrieve_All_Product_Details()
    {
        var input = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791111111111",
            NombreEs = "Leche Descremada",
            NombreEn = "Skim Milk",
            Cantidad = "1 L",
            Marca = "La Serenísima",
            ImagenUrl = "https://images.openfoodfacts.org/leche.jpg",
            Categorias = "Lácteos, Leches, lácteos",
            Ingredientes = "Leche parcialmente descremada, vitaminas A y D",
            Alergenos = "milk, MILK",
            NutriScore = "a",
            Nova = 1
        };

        var created = await _productoAppService.CreateAsync(input);
        var fetched = await _productoAppService.GetAsync(created.Id);

        fetched.CodigoBarras.ShouldBe("7791111111111");
        fetched.Marca.ShouldBe("La Serenísima");
        fetched.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/leche.jpg");
        fetched.Categorias.ShouldBe("Lácteos, Leches");
        fetched.Ingredientes.ShouldBe("Leche parcialmente descremada, vitaminas A y D");
        fetched.Alergenos.ShouldBe("milk");
        fetched.NutriScore.ShouldBe("A");
        fetched.Nova.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Preserve_Null_Details_When_Missing_On_Create()
    {
        var input = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791111111112",
            NombreEs = "Sal Fina"
        };

        var created = await _productoAppService.CreateAsync(input);
        var fetched = await _productoAppService.GetAsync(created.Id);

        fetched.Cantidad.ShouldBeNull();
        fetched.Marca.ShouldBeNull();
        fetched.ImagenUrl.ShouldBeNull();
        fetched.Categorias.ShouldBeNull();
        fetched.Ingredientes.ShouldBeNull();
        fetched.Alergenos.ShouldBeNull();
        fetched.NutriScore.ShouldBeNull();
        fetched.Nova.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Reject_Invalid_NutriScore_And_Nova_On_Create()
    {
        var invalidNutriScore = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791111111113",
            NombreEs = "Producto Test",
            NutriScore = "Z"
        };

        await Should.ThrowAsync<AbpValidationException>(() => _productoAppService.CreateAsync(invalidNutriScore));

        var invalidNova = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791111111114",
            NombreEs = "Producto Test",
            Nova = 5
        };

        await Should.ThrowAsync<AbpValidationException>(() => _productoAppService.CreateAsync(invalidNova));
    }

    [Fact]
    public async Task Should_Reject_Duplicate_CodigoBarras_On_Create()
    {
        const string barcode = "7792222222221";

        await _productoAppService.CreateAsync(new CreateUpdateProductoDto
        {
            CodigoBarras = barcode,
            NombreEs = "Yerba Mate"
        });

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _productoAppService.CreateAsync(new CreateUpdateProductoDto
            {
                CodigoBarras = $"  {barcode}  ",
                NombreEs = "Otra Yerba Mate"
            }));

        exception.Code.ShouldBe(SmartPantryDomainErrorCodes.ProductoCodigoBarrasDuplicado);
        exception.Data["CodigoBarras"].ShouldBe(barcode);
    }

    [Fact]
    public async Task Should_Reject_Duplicate_CodigoBarras_On_Update_But_Allow_Keeping_Own_Barcode()
    {
        const string barcodeA = "7792222222222";
        const string barcodeB = "7792222222223";

        var productoA = await _productoAppService.CreateAsync(new CreateUpdateProductoDto
        {
            CodigoBarras = barcodeA,
            NombreEs = "Producto A"
        });

        var productoB = await _productoAppService.CreateAsync(new CreateUpdateProductoDto
        {
            CodigoBarras = barcodeB,
            NombreEs = "Producto B"
        });

        // Intentar cambiar B al código de A debe fallar con BusinessException
        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _productoAppService.UpdateAsync(productoB.Id, new CreateUpdateProductoDto
            {
                CodigoBarras = barcodeA,
                NombreEs = "Producto B Modificado"
            }));

        exception.Code.ShouldBe(SmartPantryDomainErrorCodes.ProductoCodigoBarrasDuplicado);
        exception.Data["CodigoBarras"].ShouldBe(barcodeA);

        // Actualizar A conservando su propio código debe funcionar
        var updatedA = await _productoAppService.UpdateAsync(productoA.Id, new CreateUpdateProductoDto
        {
            CodigoBarras = barcodeA,
            NombreEs = "Producto A Actualizado",
            Marca = "Marca Nueva",
            NutriScore = "c",
            Nova = 3
        });

        updatedA.NombreEs.ShouldBe("Producto A Actualizado");
        updatedA.Marca.ShouldBe("Marca Nueva");
        updatedA.NutriScore.ShouldBe("C");
        updatedA.Nova.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Enforce_Unique_CodigoBarras_Index_At_Database_Level()
    {
        const string barcode = "7793333333331";

        await WithUnitOfWorkAsync(async () =>
        {
            await _productoRepository.InsertAsync(
                new Producto(Guid.NewGuid(), "Producto DB 1", null, barcode),
                autoSave: true
            );
        });

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await _productoRepository.InsertAsync(
                    new Producto(Guid.NewGuid(), "Producto DB 2", null, barcode),
                    autoSave: true
                );
            });
        });
    }

    [Fact]
    public async Task SaveFromExternalAsync_Should_Persist_New_Producto_And_Return_Existing_Without_Duplicating()
    {
        const string barcode = "3017620422099";
        _externalProductCatalogClient.ClearReceivedCalls();

        _externalProductCatalogClient.GetByBarcodeAsync(barcode).Returns(Task.FromResult<ExternalProductDto?>(
            new ExternalProductDto
            {
                CodigoBarras = barcode,
                Nombre = "Hazelnut Spread",
                NombreEs = "Crema de Avellanas",
                Cantidad = "400 g",
                Marca = "Ferrero",
                ImagenUrl = "https://images.openfoodfacts.org/hazelnut.jpg",
                Categorias = "Untables, Dulces",
                Ingredientes = "Azúcar, avellanas",
                Alergenos = "milk, nuts",
                NutriScore = "E",
                Nova = null
            }));

        // 1. Primera llamada: consulta catálogo externo, persiste y retorna DTO completo preservando null en Nova
        var firstResult = await _productoAppService.SaveFromExternalAsync(new SearchProductInputDto
        {
            CodigoBarras = barcode
        });

        firstResult.Id.ShouldNotBe(Guid.Empty);
        firstResult.CodigoBarras.ShouldBe(barcode);
        firstResult.NombreEs.ShouldBe("Crema de Avellanas");
        firstResult.NombreEn.ShouldBe("Hazelnut Spread");
        firstResult.Cantidad.ShouldBe("400 g");
        firstResult.Marca.ShouldBe("Ferrero");
        firstResult.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/hazelnut.jpg");
        firstResult.Categorias.ShouldBe("Untables, Dulces");
        firstResult.Ingredientes.ShouldBe("Azúcar, avellanas");
        firstResult.Alergenos.ShouldBe("milk, nuts");
        firstResult.NutriScore.ShouldBe("E");
        firstResult.Nova.ShouldBeNull();

        await _externalProductCatalogClient.Received(1).GetByBarcodeAsync(barcode);

        // 2. Segunda llamada con el mismo código: retorna el existente y no vuelve a llamar a OFF ni duplica
        _externalProductCatalogClient.ClearReceivedCalls();

        var secondResult = await _productoAppService.SaveFromExternalAsync(new SearchProductInputDto
        {
            CodigoBarras = barcode
        });

        secondResult.Id.ShouldBe(firstResult.Id);
        secondResult.CodigoBarras.ShouldBe(barcode);
        secondResult.Marca.ShouldBe("Ferrero");

        await _externalProductCatalogClient.DidNotReceive().GetByBarcodeAsync(barcode);

        var countInDb = await WithUnitOfWorkAsync(() =>
            _productoRepository.CountAsync(p => p.CodigoBarras == barcode));
        countInDb.ShouldBe(1);
    }

    [Fact]
    public async Task SaveFromExternalAsync_Should_Throw_And_Not_Persist_When_External_Product_Not_Found()
    {
        const string barcode = "3017620422098";
        _externalProductCatalogClient.GetByBarcodeAsync(barcode).Returns(Task.FromResult<ExternalProductDto?>(null));

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _productoAppService.SaveFromExternalAsync(new SearchProductInputDto { CodigoBarras = barcode }));

        var exists = await WithUnitOfWorkAsync(() =>
            _productoRepository.AnyAsync(p => p.CodigoBarras == barcode));
        exists.ShouldBeFalse();
    }
}
