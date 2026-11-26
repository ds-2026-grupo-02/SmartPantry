using System;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Productos;

public class Producto : AggregateRoot<Guid>
{
    public string? NombreEs { get; private set; }
    public string? NombreEn { get; private set; }
    public string? Cantidad { get; private set; }
    public string CodigoBarras { get; private set; } = default!;
    public string? Marca { get; private set; }
    public string? ImagenUrl { get; private set; }
    public string? Categorias { get; private set; }
    public string? Ingredientes { get; private set; }
    public string? Alergenos { get; private set; }
    public string? NutriScore { get; private set; }
    public int? Nova { get; private set; }

    // Constructor privado para Entity Framework Core
    private Producto() { }

    // Constructor público para instanciar la entidad
    public Producto(
        Guid id,
        string? nombreEs,
        string? nombreEn,
        string codigoBarras,
        string? cantidad = null,
        string? marca = null,
        string? imagenUrl = null,
        string? categorias = null,
        string? ingredientes = null,
        string? alergenos = null,
        string? nutriScore = null,
        int? nova = null)
        : base(id)
    {
        SetNombres(nombreEs, nombreEn);
        SetCodigoBarras(codigoBarras);
        SetCantidad(cantidad);
        SetMarca(marca);
        SetImagenUrl(imagenUrl);
        SetCategorias(categorias);
        SetIngredientes(ingredientes);
        SetAlergenos(alergenos);
        SetNutriScore(nutriScore);
        SetNova(nova);
    }

    public void ModificarDatos(
        string? nombreEs,
        string? nombreEn,
        string codigoBarras,
        string? cantidad = null,
        string? marca = null,
        string? imagenUrl = null,
        string? categorias = null,
        string? ingredientes = null,
        string? alergenos = null,
        string? nutriScore = null,
        int? nova = null)
    {
        SetNombres(nombreEs, nombreEn);
        SetCodigoBarras(codigoBarras);
        SetCantidad(cantidad);
        SetMarca(marca);
        SetImagenUrl(imagenUrl);
        SetCategorias(categorias);
        SetIngredientes(ingredientes);
        SetAlergenos(alergenos);
        SetNutriScore(nutriScore);
        SetNova(nova);
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

    public void SetMarca(string? marca)
    {
        var trimmed = string.IsNullOrWhiteSpace(marca) ? null : marca.Trim();
        if (trimmed != null && trimmed.Length > ProductoConsts.MaxMarcaLength)
        {
            throw new ArgumentException($"La marca no puede superar los {ProductoConsts.MaxMarcaLength} caracteres.");
        }
        Marca = trimmed;
    }
    public void SetImagenUrl(string? imagenUrl)
    {
        var trimmed = string.IsNullOrWhiteSpace(imagenUrl) ? null : imagenUrl.Trim();
        if (trimmed != null && trimmed.Length > ProductoConsts.MaxImagenUrlLength)
        {
            throw new ArgumentException($"La URL de la imagen no puede superar los {ProductoConsts.MaxImagenUrlLength} caracteres.");
        }
        ImagenUrl = trimmed;
    }
    public void SetCategorias(string? categorias)
    {
        var normalized = NormalizarListaSeparadaPorComas(categorias);
        if (normalized != null && normalized.Length > ProductoConsts.MaxCategoriasLength)
        {
            throw new ArgumentException($"Las categorías no pueden superar los {ProductoConsts.MaxCategoriasLength} caracteres.");
        }
        Categorias = normalized;
    }
    public void SetIngredientes(string? ingredientes)
    {
        var trimmed = string.IsNullOrWhiteSpace(ingredientes) ? null : ingredientes.Trim();
        if (trimmed != null && trimmed.Length > ProductoConsts.MaxIngredientesLength)
        {
            throw new ArgumentException($"Los ingredientes no pueden superar los {ProductoConsts.MaxIngredientesLength} caracteres.");
        }
        Ingredientes = trimmed;
    }
    public void SetAlergenos(string? alergenos)
    {
        var normalized = NormalizarListaSeparadaPorComas(alergenos);
        if (normalized != null && normalized.Length > ProductoConsts.MaxAlergenosLength)
        {
            throw new ArgumentException($"Los alérgenos no pueden superar los {ProductoConsts.MaxAlergenosLength} caracteres.");
        }
        Alergenos = normalized;
    }
    public void SetNutriScore(string? nutriScore)
    {
        var trimmed = string.IsNullOrWhiteSpace(nutriScore) ? null : nutriScore.Trim().ToUpperInvariant();
        if (trimmed != null && (trimmed.Length != 1 || trimmed[0] < 'A' || trimmed[0] > 'E'))
        {
            throw new ArgumentException("El Nutri-Score debe ser una letra entre A y E.");
        }
        NutriScore = trimmed;
    }
    public void SetNova(int? nova)
    {
        if (nova.HasValue && (nova.Value < ProductoConsts.MinNova || nova.Value > ProductoConsts.MaxNova))
        {
            throw new ArgumentException($"El grupo NOVA debe estar entre {ProductoConsts.MinNova} y {ProductoConsts.MaxNova}.");
        }
        Nova = nova;
    }
    private static string? NormalizarListaSeparadaPorComas(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }
        var items = valor
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return items.Length == 0 ? null : string.Join(", ", items);
    }
}
