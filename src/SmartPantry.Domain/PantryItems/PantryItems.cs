using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.PantryItems;

public class PantryItem : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid ProductoId { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public bool EsConsumido { get; private set; }

    protected PantryItem() { }

    public PantryItem(
        Guid id,
        Guid userId,
        Guid productoId,
        DateTime? fechaVencimiento)
        : base(id)
    {
        UserId = userId;
        ProductoId = productoId;
        FechaVencimiento = fechaVencimiento;
        EsConsumido = false;
    }

    // Método para actualizar la fecha de vencimiento desde las pruebas o el dominio
    public void UpdateExpirationDate(DateTime? newExpirationDate)
    {
        FechaVencimiento = newExpirationDate;
    }

    public void MarcarComoConsumido()
    {
        EsConsumido = true;
    }
}
