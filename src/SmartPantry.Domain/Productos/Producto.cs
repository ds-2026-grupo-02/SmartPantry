using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Productos;

public class Producto : AggregateRoot<Guid>
{
    public string? NombreEs { get; private set; }
    public string? NombreEn { get; private set; }
    public string? Cantidad { get; private set; }
    public string CodigoBarras { get; private set; } = default!;

    // Constructor privado para Entity Framework Core
    private Producto() { }

    // Constructor público para instanciar la entidad
    public Producto(
        Guid id,
        string? nombreEs,
        string? nombreEn,
        string codigoBarras,
        string? cantidad = null)
        : base(id)
    {
        SetNombres(nombreEs, nombreEn);
        SetCodigoBarras(codigoBarras);
        SetCantidad(cantidad);
    }

    public void ModificarDatos(
        string? nombreEs,
        string? nombreEn,
        string codigoBarras,
        string? cantidad = null)
    {
        SetNombres(nombreEs, nombreEn);
        SetCodigoBarras(codigoBarras);
        SetCantidad(cantidad);
    }

    public void SetNombres(string? nombreEs, string? nombreEn)
    {
        var trimmedEs = string.IsNullOrWhiteSpace(nombreEs) ? null : nombreEs.Trim();
        var trimmedEn = string.IsNullOrWhiteSpace(nombreEn) ? null : nombreEn.Trim();

        if (trimmedEs == null && trimmedEn == null)
        {
            throw new ArgumentException("El producto debe tener al menos un nombre (en español o en inglés).");
        }

        if (trimmedEs != null && trimmedEs.Length > ProductoConsts.MaxNombreLength)
        {
            throw new ArgumentException($"El nombre en español no puede superar los {ProductoConsts.MaxNombreLength} caracteres.");
        }

        if (trimmedEn != null && trimmedEn.Length > ProductoConsts.MaxNombreLength)
        {
            throw new ArgumentException($"El nombre en inglés no puede superar los {ProductoConsts.MaxNombreLength} caracteres.");
        }

        NombreEs = trimmedEs;
        NombreEn = trimmedEn;
    }

    public void SetCantidad(string? cantidad)
    {
        var trimmed = string.IsNullOrWhiteSpace(cantidad) ? null : cantidad.Trim();
        if (trimmed != null && trimmed.Length > ProductoConsts.MaxCantidadLength)
        {
            throw new ArgumentException($"La cantidad no puede superar los {ProductoConsts.MaxCantidadLength} caracteres.");
        }

        Cantidad = trimmed;
    }

    public void SetCodigoBarras(string codigoBarras)
    {
        Check.NotNullOrWhiteSpace(codigoBarras, nameof(codigoBarras));

        var trimmed = codigoBarras.Trim();
        if (trimmed.Length > ProductoConsts.MaxCodigoBarrasLength)
        {
            throw new ArgumentException($"El código de barras no puede superar los {ProductoConsts.MaxCodigoBarrasLength} caracteres.");
        }

        CodigoBarras = trimmed;
    }
}
