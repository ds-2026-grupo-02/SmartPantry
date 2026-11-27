using System;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.CatalogoExterno;

public class RegistroLlamadaExterna : Entity<Guid>
{
    public TipoOperacionCatalogoExterno TipoOperacion { get; private set; }
    public DateTime FechaHoraUtc { get; private set; }
    public long DuracionMs { get; private set; }
    public ResultadoLlamadaExterna Resultado { get; private set; }
    public int? CodigoHttp { get; private set; }

    // Constructor privado requerido por Entity Framework Core
    private RegistroLlamadaExterna() { }

    public RegistroLlamadaExterna(
        Guid id,
        TipoOperacionCatalogoExterno tipoOperacion,
        DateTime fechaHoraUtc,
        long duracionMs,
        ResultadoLlamadaExterna resultado,
        int? codigoHttp = null)
        : base(id)
    {
        if (duracionMs < 0)
        {
            throw new ArgumentException("La duración en milisegundos no puede ser negativa.", nameof(duracionMs));
        }

        if (codigoHttp.HasValue && (codigoHttp.Value < 100 || codigoHttp.Value > 599))
        {
            throw new ArgumentException("El código HTTP debe estar entre 100 y 599.", nameof(codigoHttp));
        }

        TipoOperacion = tipoOperacion;
        FechaHoraUtc = fechaHoraUtc;
        DuracionMs = duracionMs;
        Resultado = resultado;
        CodigoHttp = codigoHttp;
    }
}