using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Productos;

public class Producto : AggregateRoot<Guid>
{
    public string Nombre { get; private set; }
    public string CodigoBarras { get; private set; }

    // Constructor privado para Entity Framework Core
    private Producto() { }

    // Constructor público para instanciar la entidad
    public Producto(Guid id, string nombre, string codigoBarras)
        : base(id)
    {
        SetNombre(nombre);
        SetCodigoBarras(codigoBarras);
    }

    public void ModificarDatos(string nombre, string codigoBarras)
    {
        SetNombre(nombre);
        SetCodigoBarras(codigoBarras);
    }


    public void SetNombre(string nombre)
    {
        Check.NotNullOrWhiteSpace(nombre, nameof(nombre));

        var trimmed = nombre.Trim();
        if (trimmed.Length > ProductoConsts.MaxNombreLength)
        {
            throw new ArgumentException($"El nombre no puede superar los {ProductoConsts.MaxNombreLength} caracteres.");
        }

        Nombre = trimmed;
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