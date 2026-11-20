using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Advertencias;

public class AdvertenciaVencimiento : FullAuditedEntity<Guid>
{
    public Guid UsuarioId { get; private set; }
    public Guid ItemDespensaId { get; private set; }
    public string TipoAdvertencia { get; private set; }
    public bool EsActiva { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }

    protected AdvertenciaVencimiento() { }

    public AdvertenciaVencimiento(
        Guid id,
        Guid idUsuario,
        Guid idItemDespensa,
        string tipoAdvertencia,
        DateTime? fechaVencimiento)
        : base(id)
    {
        UsuarioId = idUsuario;
        ItemDespensaId = idItemDespensa;
        TipoAdvertencia = tipoAdvertencia;
        FechaVencimiento = fechaVencimiento;
        EsActiva = true;
    }

    public void Desactivar()
    {
        EsActiva = false;
    }

    public void Reactivar(DateTime? nuevaFechaVencimiento)
    {
        EsActiva = true;
        FechaVencimiento = nuevaFechaVencimiento;
    }

    public void ActualizarFechaVencimiento(DateTime? nuevaFechaVencimiento)
    {
        FechaVencimiento = nuevaFechaVencimiento;
    }
}