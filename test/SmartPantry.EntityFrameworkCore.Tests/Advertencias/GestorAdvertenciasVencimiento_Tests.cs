using Shouldly;
using SmartPantry.Advertencias;
using SmartPantry.Despensa;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Advertencias;

public class GestorAdvertenciasVencimiento_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly GestorAdvertenciasVencimiento _gestorAdvertencias;
    private readonly IRepository<ItemDespensa, Guid> _itemDespensaRepository;
    private readonly IRepository<AdvertenciaVencimiento, Guid> _advertenciaRepository;

    public GestorAdvertenciasVencimiento_Tests()
    {
        _gestorAdvertencias = GetRequiredService<GestorAdvertenciasVencimiento>();
        _itemDespensaRepository = GetRequiredService<IRepository<ItemDespensa, Guid>>();
        _advertenciaRepository = GetRequiredService<IRepository<AdvertenciaVencimiento, Guid>>();
    }

    // 1. REQUERIMIENTO: Probar fecha controlada UTC (Dentro/Fuera de umbral, Día límite, Vencido)
    [Theory]
    [InlineData(2, true)]   // Dentro del umbral (2 días antes) -> Genera advertencia
    [InlineData(0, true)]   // Día límite (vence hoy) -> Genera advertencia
    [InlineData(-1, false)] // Vencido (venció ayer) -> No genera advertencia
    [InlineData(10, false)] // Fuera del umbral (vence en 10 días) -> No genera advertencia
    public async Task Deberia_Manejar_Distintos_Umbrales_De_Vencimiento(int diasDesfasaje, bool deberiaEstarActiva)
    {
        var fechaReferenciaUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var fechaVencimientoUtc = fechaReferenciaUtc.AddDays(diasDesfasaje);
        var itemId = Guid.NewGuid();

        var item = new ItemDespensa(itemId, Guid.NewGuid(), Guid.NewGuid(), fechaVencimientoUtc);

        await WithUnitOfWorkAsync(async () => await _itemDespensaRepository.InsertAsync(item));

        await WithUnitOfWorkAsync(async () =>
            await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            if (deberiaEstarActiva)
            {
                advertencias.Count.ShouldBe(1);
                advertencias.First().EsActiva.ShouldBeTrue();
            }
            else
            {
                advertencias.Count(x => x.EsActiva).ShouldBe(0);
            }
        });
    }

    // 2. REQUERIMIENTO: Casos de borde (Sin fecha y Consumido)
    [Fact]
    public async Task Deberia_Ignorar_Items_Consumidos_O_Sin_Fecha()
    {
        var fechaReferenciaUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);

        // Ítem sin fecha de vencimiento
        var itemSinFecha = new ItemDespensa(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

        // Ítem consumido (invocando el método de dominio)
        var itemConsumido = new ItemDespensa(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), fechaReferenciaUtc.AddDays(1));
        itemConsumido.MarcarComoConsumido();

        await WithUnitOfWorkAsync(async () =>
        {
            await _itemDespensaRepository.InsertAsync(itemSinFecha);
            await _itemDespensaRepository.InsertAsync(itemConsumido);
        });

        await WithUnitOfWorkAsync(async () =>
            await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var advertenciasSinFecha = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemSinFecha.Id);
            var advertenciasConsumido = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemConsumido.Id);

            advertenciasSinFecha.Count.ShouldBe(0);
            advertenciasConsumido.Count.ShouldBe(0);
        });
    }

    // 3. REQUERIMIENTO: Demostrar Idempotencia (Doble ejecución sobre los mismos datos)
    [Fact]
    public async Task Deberia_Ser_Idempotente_Al_Ejecutarse_Multiples_Veces()
    {
        var fechaReferenciaUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();
        var item = new ItemDespensa(itemId, Guid.NewGuid(), Guid.NewGuid(), fechaReferenciaUtc.AddDays(1));

        await WithUnitOfWorkAsync(async () => await _itemDespensaRepository.InsertAsync(item));

        // Corrida 1
        await WithUnitOfWorkAsync(async () =>
            await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // Corrida 2 (Ejecución repetida)
        await WithUnitOfWorkAsync(async () =>
            await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1); // Se mantiene una sola advertencia por ítem
        });
    }

    // 4. REQUERIMIENTO: Ciclo completo (Actualización, Desactivación y Reactivación)
    [Fact]
    public async Task Deberia_Actualizar_Fecha_De_Advertencia_Cuando_Cambia_Fecha_Del_Item()
    {
        var fechaReferenciaUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();
        var vencimientoInicial = fechaReferenciaUtc.AddDays(1);
        var vencimientoActualizado = fechaReferenciaUtc.AddDays(2); // Ambas dentro del umbral de 3 días

        var item = new ItemDespensa(itemId, Guid.NewGuid(), Guid.NewGuid(), vencimientoInicial);
        await WithUnitOfWorkAsync(async () => await _itemDespensaRepository.InsertAsync(item));

        // 1. Generar advertencia inicial
        await WithUnitOfWorkAsync(async () => await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // 2. Cambiar la fecha de vencimiento del ítem (sigue dentro del umbral)
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _itemDespensaRepository.GetAsync(itemId);
            dbItem.ActualizarFechaVencimiento(vencimientoActualizado);
            await _itemDespensaRepository.UpdateAsync(dbItem);
        });

        // 3. Procesar nuevamente
        await WithUnitOfWorkAsync(async () => await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // 4. Comprobar que la advertencia actualizó su fecha y sigue siendo única
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1);
            advertencias.First().FechaVencimiento.ShouldBe(vencimientoActualizado);
        });
    }

    // Cuando un producto con advertencia activa se marca como consumido,
    // la advertencia pasa efectivamente a EsActiva = false.
    [Fact]
    public async Task Deberia_Desactivar_Advertencia_Cuando_El_Item_Se_Marca_Como_Consumido()
    {
        var fechaReferenciaUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();

        // 1. Ítem activo dentro del umbral -> Genera advertencia
        var item = new ItemDespensa(itemId, Guid.NewGuid(), Guid.NewGuid(), fechaReferenciaUtc.AddDays(2));
        await WithUnitOfWorkAsync(async () => await _itemDespensaRepository.InsertAsync(item));
        await WithUnitOfWorkAsync(async () => await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // Verificamos que la advertencia esté activa
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1);
            advertencias.First().EsActiva.ShouldBeTrue();
        });

        // 2. Se consume el ítem -> Se ejecuta el gestor nuevamente
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _itemDespensaRepository.GetAsync(itemId);
            dbItem.MarcarComoConsumido();
            await _itemDespensaRepository.UpdateAsync(dbItem);
        });
        await WithUnitOfWorkAsync(async () => await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // 3. Comprobamos que la advertencia fue desactivada
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1);
            advertencias.First().EsActiva.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Deberia_Desactivar_Advertencia_Cuando_El_Item_Queda_Sin_Fecha()
    {
        var fechaReferenciaUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();
        var itemDespensa = new ItemDespensa(itemId, Guid.NewGuid(), Guid.NewGuid(), fechaReferenciaUtc.AddDays(2));

        await WithUnitOfWorkAsync(async () =>
        {
            await _itemDespensaRepository.InsertAsync(itemDespensa);
        });

        // 1. Generar advertencia inicial
        await WithUnitOfWorkAsync(async () =>
            await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // 2. Quitamos la fecha de vencimiento (ítem sin fecha: null)
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _itemDespensaRepository.GetAsync(itemId);
            dbItem.ActualizarFechaVencimiento(null);
            await _itemDespensaRepository.UpdateAsync(dbItem);
        });

        // 3. Procesar nuevamente
        await WithUnitOfWorkAsync(async () =>
            await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // 4. Comprobamos que la advertencia se desactiva
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1);
            advertencias.First().EsActiva.ShouldBeFalse();
        });
    }

    // Reactivación de una advertencia previamente desactivada
    [Fact]
    public async Task Deberia_Reactivar_Advertencia_Desactivada_Cuando_Vuelve_Al_Umbral()
    {
        var fechaReferenciaUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var itemId = Guid.NewGuid();

        // Paso 1: Ítem próximo a vencer (+1 día) -> Se genera advertencia activa
        var item = new ItemDespensa(itemId, Guid.NewGuid(), Guid.NewGuid(), fechaReferenciaUtc.AddDays(1));
        await WithUnitOfWorkAsync(async () => await _itemDespensaRepository.InsertAsync(item));
        await WithUnitOfWorkAsync(async () => await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1);
            advertencias.First().EsActiva.ShouldBeTrue();
        });

        // Paso 2: Se posterga la fecha fuera del umbral (+20 días) -> La advertencia se desactiva
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _itemDespensaRepository.GetAsync(itemId);
            dbItem.ActualizarFechaVencimiento(fechaReferenciaUtc.AddDays(20));
            await _itemDespensaRepository.UpdateAsync(dbItem);
        });
        await WithUnitOfWorkAsync(async () => await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1);
            advertencias.First().EsActiva.ShouldBeFalse(); // Comprobamos que quedó desactivada
        });

        // Paso 3: La fecha vuelve a estar dentro del umbral (+2 días) -> La MISMA advertencia debe reactivarse
        var fechaReactivacion = fechaReferenciaUtc.AddDays(2);
        await WithUnitOfWorkAsync(async () =>
        {
            var dbItem = await _itemDespensaRepository.GetAsync(itemId);
            dbItem.ActualizarFechaVencimiento(fechaReactivacion);
            await _itemDespensaRepository.UpdateAsync(dbItem);
        });
        await WithUnitOfWorkAsync(async () => await _gestorAdvertencias.ProcessExpirationsAsync(fechaReferenciaUtc, thresholdDays: 3));

        // Paso 4: Verificamos que se reactivó y que no se duplicó el registro en la base
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencias = await _advertenciaRepository.GetListAsync(x => x.ItemDespensaId == itemId);
            advertencias.Count.ShouldBe(1); // Sigue existiendo un único registro (idempotencia)
            advertencias.First().EsActiva.ShouldBeTrue(); // Reactivada
            advertencias.First().FechaVencimiento.ShouldBe(fechaReactivacion); // Con la nueva fecha
        });
    }
}
