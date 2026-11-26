using System;
using Shouldly;
using SmartPantry.CatalogoExterno;
using Xunit;

namespace SmartPantry.CatalogoExterno;

public class RegistroLlamadaExternaTests
{
    [Fact]
    public void Debe_Crear_Registro_Valido()
    {
        var id = Guid.NewGuid();
        var fecha = DateTime.UtcNow;

        var registro = new RegistroLlamadaExterna(
            id,
            TipoOperacionCatalogoExterno.BusquedaPorCodigo,
            fecha,
            145,
            ResultadoLlamadaExterna.Success,
            200
        );

        registro.Id.ShouldBe(id);
        registro.TipoOperacion.ShouldBe(TipoOperacionCatalogoExterno.BusquedaPorCodigo);
        registro.FechaHoraUtc.ShouldBe(fecha);
        registro.DuracionMs.ShouldBe(145);
        registro.Resultado.ShouldBe(ResultadoLlamadaExterna.Success);
        registro.CodigoHttp.ShouldBe(200);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-500)]
    public void Debe_Lanzar_Excepcion_Si_Duracion_Es_Negativa(long duracionInvalida)
    {
        Should.Throw<ArgumentException>(() =>
            new RegistroLlamadaExterna(
                Guid.NewGuid(),
                TipoOperacionCatalogoExterno.BusquedaPorTexto,
                DateTime.UtcNow,
                duracionInvalida,
                ResultadoLlamadaExterna.Error
            )
        );
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void Debe_Lanzar_Excepcion_Si_CodigoHttp_Es_Invalido(int codigoInvalido)
    {
        Should.Throw<ArgumentException>(() =>
            new RegistroLlamadaExterna(
                Guid.NewGuid(),
                TipoOperacionCatalogoExterno.BusquedaPorCodigo,
                DateTime.UtcNow,
                50,
                ResultadoLlamadaExterna.Error,
                codigoInvalido
            )
        );
    }
}