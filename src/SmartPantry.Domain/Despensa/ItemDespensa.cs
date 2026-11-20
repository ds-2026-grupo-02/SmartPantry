using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Despensa;

public class ItemDespensa : FullAuditedAggregateRoot<Guid>
{
    public Guid UsuarioId { get; private set; }
    public Guid ProductoId { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public bool EsConsumido { get; private set; }

    protected ItemDespensa() { }

    public ItemDespensa(
        Guid id,
        Guid usuarioId,
        Guid productoId,
        DateTime? fechaVencimiento)
        : base(id)
    {
        UsuarioId = usuarioId;
        ProductoId = productoId;
        FechaVencimiento = fechaVencimiento;
        EsConsumido = false;
    }

    // Método para actualizar la fecha de vencimiento desde las pruebas o el dominio
    public void ActualizarFechaVencimiento(DateTime? nuevaFechaVencimiento)
    {
        FechaVencimiento = nuevaFechaVencimiento;
    }

    public void MarcarComoConsumido()
    {
        EsConsumido = true;
    }
}
