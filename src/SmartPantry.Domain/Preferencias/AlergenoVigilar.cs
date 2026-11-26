using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace SmartPantry.Preferencias;

public class AlergenoVigilar : ValueObject
{
    public string Nombre { get; private set; } = default!;

    private AlergenoVigilar() { }

    public AlergenoVigilar(string nombre)
    {
        Nombre = NombreVigilado.Normalizar(nombre);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Nombre.ToUpperInvariant();
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && ValueEquals(other);
    }

    public override int GetHashCode()
    {
        return Nombre.ToUpperInvariant().GetHashCode();
    }
}
