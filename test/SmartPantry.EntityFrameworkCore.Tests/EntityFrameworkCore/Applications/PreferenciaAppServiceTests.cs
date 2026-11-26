using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Applications;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class PreferenciaAppServiceTests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly SmartPantry.Preferencias.IPreferenciaAppService _preferenciaAppService;
    private readonly IRepository<SmartPantry.Preferencias.PreferenciaUsuario, Guid> _preferenciaRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public PreferenciaAppServiceTests()
    {
        _preferenciaAppService = GetRequiredService<SmartPantry.Preferencias.IPreferenciaAppService>();
        _preferenciaRepository = GetRequiredService<IRepository<SmartPantry.Preferencias.PreferenciaUsuario, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private IDisposable ChangeUser(Guid userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, "test-" + userId),
            new Claim(AbpClaimTypes.Email, $"test-{userId}@test.com")
        }, "FakeAuth"));

        return _currentPrincipalAccessor.Change(principal);
    }

    // a) GetAsync de usuario sin configurar devuelve listas vacías y MostrarAlertas=true,
    //    y NO crea ninguna fila en la base de datos.
    [Fact]
    public async Task GetAsync_UsuarioSinConfigurar_DevuelveDefaults_SinCrearFila()
    {
        var userId = Guid.NewGuid();

        SmartPantry.Preferencias.PreferenciaDto resultado;
        using (ChangeUser(userId))
        {
            resultado = await _preferenciaAppService.GetAsync();
        }

        resultado.Alergenos.ShouldBeEmpty();
        resultado.Ingredientes.ShouldBeEmpty();
        resultado.MostrarAlertas.ShouldBeTrue();

        // Verificar que NO se insertó ninguna fila (dentro de un UoW para evitar DbContext disposed)
        await WithUnitOfWorkAsync(async () =>
        {
            var filas = await _preferenciaRepository.GetListAsync(p => p.UsuarioId == userId);
            filas.ShouldBeEmpty();
        });
    }

    // b) UpdateAsync y luego GetAsync devuelven lo guardado, normalizado (trim).
    [Fact]
    public async Task UpdateAsync_LuegoGetAsync_DevuelveLoGuardadoNormalizado()
    {
        var userId = Guid.NewGuid();

        SmartPantry.Preferencias.PreferenciaDto resultado;
        using (ChangeUser(userId))
        {
            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "  Gluten  ", "Soja " },
                Ingredientes = new List<string> { " Azúcar" },
                MostrarAlertas = false
            });

            resultado = await _preferenciaAppService.GetAsync();
        }

        resultado.Alergenos.ShouldBe(new[] { "Gluten", "Soja" });
        resultado.Ingredientes.ShouldBe(new[] { "Azúcar" });
        resultado.MostrarAlertas.ShouldBeFalse();
    }

    // c) AISLAMIENTO: usuario1 guarda "Gluten"; usuario2 ve todo vacío;
    //    usuario2 guarda "Lactosa"; usuario1 sigue viendo solo "Gluten".
    [Fact]
    public async Task UpdateAsync_AislamientoEntreUsuarios()
    {
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        // Usuario 1 guarda "Gluten"
        using (ChangeUser(user1Id))
        {
            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Gluten" },
                MostrarAlertas = true
            });
        }

        // Usuario 2 hace GetAsync y ve todo vacío
        SmartPantry.Preferencias.PreferenciaDto resultadoUser2Get;
        using (ChangeUser(user2Id))
        {
            resultadoUser2Get = await _preferenciaAppService.GetAsync();
        }
        resultadoUser2Get.Alergenos.ShouldBeEmpty();
        resultadoUser2Get.Ingredientes.ShouldBeEmpty();

        // Usuario 2 guarda "Lactosa"
        using (ChangeUser(user2Id))
        {
            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Lactosa" },
                MostrarAlertas = true
            });
        }

        // Usuario 1 sigue viendo solo "Gluten"
        SmartPantry.Preferencias.PreferenciaDto resultadoUser1;
        using (ChangeUser(user1Id))
        {
            resultadoUser1 = await _preferenciaAppService.GetAsync();
        }
        resultadoUser1.Alergenos.ShouldBe(new[] { "Gluten" });
        resultadoUser1.Ingredientes.ShouldBeEmpty();
    }

    // d) Guardar dos veces la misma lista no falla y deja exactamente esos elementos.
    [Fact]
    public async Task UpdateAsync_MismaListaDosVeces_NoFallaYMantieneDosElementos()
    {
        var userId = Guid.NewGuid();

        SmartPantry.Preferencias.PreferenciaDto resultado;
        using (ChangeUser(userId))
        {
            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Gluten", "Lactosa" },
                MostrarAlertas = true
            });

            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Gluten", "Lactosa" },
                MostrarAlertas = true
            });

            resultado = await _preferenciaAppService.GetAsync();
        }

        resultado.Alergenos.Count.ShouldBe(2);
        resultado.Alergenos.ShouldContain("Gluten");
        resultado.Alergenos.ShouldContain("Lactosa");
    }

    // e) Guardar lista con duplicados lanza BusinessException con Code PreferenciaDuplicado,
    //    y el estado previo se mantiene.
    [Fact]
    public async Task UpdateAsync_ListaConDuplicados_LanzaBusinessException_YMantieneEstadoPrevio()
    {
        var userId = Guid.NewGuid();

        // Primero guardar un estado válido
        using (ChangeUser(userId))
        {
            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Maní" },
                MostrarAlertas = false
            });
        }

        // Intentar actualizar con duplicados
        using (ChangeUser(userId))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
                {
                    Alergenos = new List<string> { "Gluten", "GLUTEN" },
                    MostrarAlertas = true
                });
            });

            ex.Code.ShouldBe(SmartPantryDomainErrorCodes.PreferenciaDuplicado);
        }

        // El estado previo debe mantenerse
        SmartPantry.Preferencias.PreferenciaDto resultadoDespues;
        using (ChangeUser(userId))
        {
            resultadoDespues = await _preferenciaAppService.GetAsync();
        }

        resultadoDespues.Alergenos.ShouldBe(new[] { "Maní" });
        resultadoDespues.MostrarAlertas.ShouldBeFalse();
    }

    // f) Después de varios UpdateAsync del mismo usuario existe UNA sola fila de PreferenciaUsuario.
    [Fact]
    public async Task UpdateAsync_VariasVeces_ExisteUnaSolaFila()
    {
        var userId = Guid.NewGuid();

        using (ChangeUser(userId))
        {
            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Gluten" },
                MostrarAlertas = true
            });

            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Lactosa" },
                MostrarAlertas = false
            });

            await _preferenciaAppService.UpdateAsync(new SmartPantry.Preferencias.UpdatePreferenciaDto
            {
                Alergenos = new List<string> { "Soja", "Huevo" },
                MostrarAlertas = true
            });
        }

        var filas = await _preferenciaRepository.GetListAsync(p => p.UsuarioId == userId);
        filas.Count.ShouldBe(1);
    }
}
