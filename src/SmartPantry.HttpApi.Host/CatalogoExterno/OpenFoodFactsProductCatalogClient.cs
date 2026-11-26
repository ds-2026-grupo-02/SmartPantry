using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartPantry.Productos;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace SmartPantry.CatalogoExterno;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly IRepository<RegistroLlamadaExterna, Guid> _registroRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly ILogger<OpenFoodFactsProductCatalogClient> _logger;

    // Inyectamos el HttpClient que será configurado mediante IHttpClientFactory
    public OpenFoodFactsProductCatalogClient(
        HttpClient httpClient,
        IRepository<RegistroLlamadaExterna, Guid> registroRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IGuidGenerator guidGenerator,
        IClock clock,
        ILogger<OpenFoodFactsProductCatalogClient> logger)
    {
        _httpClient = httpClient;
        _registroRepository = registroRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        const string fields = "code,product_name,product_name_en,product_name_es,quantity,brands,image_front_url,categories,categories_tags,ingredients_text_es,ingredients_text_en,ingredients_text,allergens,allergens_tags,nutriscore_grade,nova_group";
        var requestUri = $"product/{Uri.EscapeDataString(barcode)}?fields={Uri.EscapeDataString(fields)}";

        var fechaInicioUtc = NormalizarComoUtc(_clock.Now);
        var stopwatch = Stopwatch.StartNew();
        var resultado = ResultadoLlamadaExterna.Error;
        int? codigoHttp = null;

        try
        {
            using var response = await _httpClient.GetAsync(requestUri);
            codigoHttp = (int)response.StatusCode;

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                resultado = ResultadoLlamadaExterna.NotFound;
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                resultado = ResultadoLlamadaExterna.RateLimited;
                throw new UserFriendlyException("Open Food Facts limitó temporalmente las consultas. Por favor, intente más tarde.");
            }

            if (!response.IsSuccessStatusCode)
            {
                resultado = ResultadoLlamadaExterna.Error;
                throw new UserFriendlyException($"Open Food Facts devolvió un error inesperado (HTTP {(int)response.StatusCode}).");
            }

            var offResponse = await response.Content.ReadFromJsonAsync<OpenFoodFactsApiResponse>();

            if (offResponse == null || offResponse.Status != "success" || offResponse.Product == null)
            {
                resultado = ResultadoLlamadaExterna.NotFound;
                return null;
            }

            var product = offResponse.Product;

            var nombreEn = TrimOrNull(product.ProductNameEn) ?? TrimOrNull(product.ProductName);

            var ingredientes = TrimOrNull(product.IngredientsTextEs)
                ?? TrimOrNull(product.IngredientsTextEn)
                ?? TrimOrNull(product.IngredientsText);

            resultado = ResultadoLlamadaExterna.Success;

            return new ExternalProductDto
            {
                CodigoBarras = TrimOrNull(product.Code) ?? barcode.Trim(),
                Nombre = nombreEn,
                NombreEs = TrimOrNull(product.ProductNameEs),
                Cantidad = TrimOrNull(product.Quantity),
                Marca = TrimOrNull(product.Brands),
                ImagenUrl = TrimOrNull(product.ImageFrontUrl),
                Categorias = NormalizarLista(product.Categories) ?? NormalizarTags(product.CategoriesTags),
                Ingredientes = ingredientes,
                Alergenos = NormalizarTags(product.AllergensTags) ?? NormalizarLista(product.Allergens),
                NutriScore = NormalizarNutriScore(product.NutriScoreGrade),
                Nova = NormalizarNova(product.NovaGroup)
            };
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            resultado = ResultadoLlamadaExterna.Timeout;
            throw new UserFriendlyException("La consulta a Open Food Facts excedió el tiempo de espera.");
        }
        catch (HttpRequestException)
        {
            resultado = ResultadoLlamadaExterna.Error;
            throw new UserFriendlyException("No se pudo conectar con el catálogo externo de Open Food Facts.");
        }
        catch (Exception)
        {
            resultado = ResultadoLlamadaExterna.Error;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await RegistrarLlamadaAsync(
                TipoOperacionCatalogoExterno.BusquedaPorCodigo,
                fechaInicioUtc,
                stopwatch.ElapsedMilliseconds,
                resultado,
                codigoHttp
            );
        }
    }

    private async Task RegistrarLlamadaAsync(
        TipoOperacionCatalogoExterno tipoOperacion,
        DateTime fechaHoraUtc,
        long duracionMs,
        ResultadoLlamadaExterna resultado,
        int? codigoHttp)
    {
        try
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

            var registro = new RegistroLlamadaExterna(
                _guidGenerator.Create(),
                tipoOperacion,
                fechaHoraUtc,
                duracionMs,
                resultado,
                codigoHttp
            );

            await _registroRepository.InsertAsync(registro, autoSave: true);
            await uow.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo registrar la métrica de llamada externa (Operación: {TipoOperacion}, Resultado: {Resultado}, CodigoHttp: {CodigoHttp}).",
                tipoOperacion,
                resultado,
                codigoHttp);
        }
    }

    private static DateTime NormalizarComoUtc(DateTime fecha)
    {
        return fecha.Kind switch
        {
            DateTimeKind.Utc => fecha,
            DateTimeKind.Local => fecha.ToUniversalTime(),
            _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
        };
    }

    private static string? TrimOrNull(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static string? NormalizarNutriScore(string? grade)
    {
        var trimmed = TrimOrNull(grade)?.ToUpperInvariant();
        if (trimmed != null && trimmed.Length == 1 && trimmed[0] >= 'A' && trimmed[0] <= 'E')
        {
            return trimmed;
        }

        return null;
    }

    private static int? NormalizarNova(int? novaGroup)
    {
        return novaGroup.HasValue &&
               novaGroup.Value >= ProductoConsts.MinNova &&
               novaGroup.Value <= ProductoConsts.MaxNova
            ? novaGroup.Value
            : null;
    }

    private static string? NormalizarLista(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var items = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(LimpiarTag)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return items.Length == 0 ? null : string.Join(", ", items);
    }

    private static string? NormalizarTags(IEnumerable<string>? tags)
    {
        if (tags == null)
        {
            return null;
        }

        var items = tags
            .Select(LimpiarTag)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return items.Length == 0 ? null : string.Join(", ", items);
    }

    private static string LimpiarTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        var limpio = tag.Trim();
        var colonIndex = limpio.IndexOf(':');
        if (colonIndex > 0 && colonIndex < limpio.Length - 1)
        {
            limpio = limpio[(colonIndex + 1)..];
        }

        return limpio.Replace('-', ' ').Trim();
    }

    #region Clases privadas para desmaterializar el JSON crudo de Open Food Facts
    private class OpenFoodFactsApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("product")]
        public OpenFoodFactsProductData? Product { get; set; }
    }

    private class OpenFoodFactsProductData
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("product_name_en")]
        public string? ProductNameEn { get; set; }

        [JsonPropertyName("product_name_es")]
        public string? ProductNameEs { get; set; }

        [JsonPropertyName("quantity")]
        public string? Quantity { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("image_front_url")]
        public string? ImageFrontUrl { get; set; }

        [JsonPropertyName("categories")]
        public string? Categories { get; set; }

        [JsonPropertyName("categories_tags")]
        public List<string>? CategoriesTags { get; set; }

        [JsonPropertyName("ingredients_text_es")]
        public string? IngredientsTextEs { get; set; }

        [JsonPropertyName("ingredients_text_en")]
        public string? IngredientsTextEn { get; set; }

        [JsonPropertyName("ingredients_text")]
        public string? IngredientsText { get; set; }

        [JsonPropertyName("allergens")]
        public string? Allergens { get; set; }

        [JsonPropertyName("allergens_tags")]
        public List<string>? AllergensTags { get; set; }

        [JsonPropertyName("nutriscore_grade")]
        public string? NutriScoreGrade { get; set; }

        [JsonPropertyName("nova_group")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? NovaGroup { get; set; }
    }
    #endregion
}