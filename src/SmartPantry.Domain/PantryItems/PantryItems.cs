using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.PantryItems;

public class PantryItem : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid ProductoId { get; private set; }
    public decimal? Cantidad { get; private set; }
    public string? Unidad { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public bool EsConsumido { get; private set; }

    protected PantryItem() { }

    public PantryItem(
        Guid id,
        Guid userId,
        Guid productoId,
        decimal? cantidad,
        string? unidad,
        DateTime? fechaVencimiento)
        : base(id)
    {
        UserId = userId;
        ProductoId = productoId;
        SetCantidadYUnidad(cantidad, unidad);
        FechaVencimiento = fechaVencimiento;
        EsConsumido = false;
    }

    // Método para actualizar la fecha de vencimiento desde las pruebas o el dominio
    public void UpdateExpirationDate(DateTime? newExpirationDate)
    {
        FechaVencimiento = newExpirationDate;
    }

    public void SetCantidadYUnidad(decimal? cantidad, string? unidad)
    {
        if (cantidad.HasValue && cantidad.Value < 0)
        {
            throw new BusinessException("SmartPantry:CantidadInvalida")
                .WithData("Cantidad", cantidad);
        }

        var trimmedUnidad = string.IsNullOrWhiteSpace(unidad) ? null : unidad.Trim();
        if (trimmedUnidad != null && trimmedUnidad.Length > PantryItemConsts.MaxUnidadLength)
        {
            throw new ArgumentException($"La unidad no puede superar los {PantryItemConsts.MaxUnidadLength} caracteres.");
        }

        Cantidad = cantidad;
        Unidad = trimmedUnidad;
    }

    public void MarcarComoConsumido()
    {
        EsConsumido = true;
        Cantidad = 0;
    }
}
