using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPantry.Productos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace SmartPantry.CatalogoExterno;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private const string BarcodeFields =
        "code,product_name,product_name_en,product_name_es,quantity,brands,image_front_url,categories,categories_tags,ingredients_text_es,ingredients_text_en,ingredients_text,allergens,allergens_tags,nutriscore_grade,nova_group";

    private static readonly string[] SearchFields =
    {
        "code",
        "product_name",
        "product_name_en",
        "product_name_es",
        "quantity",
        "brands",
        "image_front_url",
        "image_url",
        "categories",
        "categories_tags",
        "ingredients_text_es",
        "ingredients_text_en",
        "ingredients_text",
        "allergens",
        "allergens_tags",
        "nutriscore_grade",
        "nova_group"
    };

    private readonly HttpClient _httpClient;
    private readonly IRepository<RegistroLlamadaExterna, Guid> _registroRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly ILogger<OpenFoodFactsProductCatalogClient> _logger;
    private readonly OpenFoodFactsOptions _options;

    // Inyectamos el HttpClient que será configurado mediante IHttpClientFactory
    public OpenFoodFactsProductCatalogClient(
        HttpClient httpClient,
        IRepository<RegistroLlamadaExterna, Guid> registroRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IGuidGenerator guidGenerator,
        IClock clock,
        ILogger<OpenFoodFactsProductCatalogClient> logger,
        IOptions<OpenFoodFactsOptions>? options = null)
    {
        _httpClient = httpClient;
        _registroRepository = registroRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _logger = logger;
        _options = options?.Value ?? new OpenFoodFactsOptions();
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var barcodeLimpio = barcode.Trim();
        var requestUri = $"product/{Uri.EscapeDataString(barcodeLimpio)}?fields={Uri.EscapeDataString(BarcodeFields)}";

        return await EjecutarConMetricasAsync(
            TipoOperacionCatalogoExterno.BusquedaPorCodigo,
            async contexto =>
            {
                using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
                contexto.CodigoHttp = (int)response.StatusCode;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    contexto.Resultado = ResultadoLlamadaExterna.NotFound;
                    return null;
                }

                ValidarRespuestaHttp(response, contexto);

                var offResponse = await response.Content.ReadFromJsonAsync<OpenFoodFactsApiResponse>(
                    cancellationToken: cancellationToken);

                if (offResponse == null || offResponse.Status != "success" || offResponse.Product == null)
                {
                    contexto.Resultado = ResultadoLlamadaExterna.NotFound;
                    return null;
                }

                contexto.Resultado = ResultadoLlamadaExterna.Success;
                return MapearProductoBarcode(offResponse.Product, barcodeLimpio);
            },
            cancellationToken);
    }

    public async Task<ResultadoBusquedaCatalogoExternoDto> SearchByTextAsync(
        string query,
        int pagina = BuscarProductoPorTextoInputDto.PaginaPorDefecto,
        int tamanioPagina = BuscarProductoPorTextoInputDto.TamanioPaginaPorDefecto,
        IReadOnlyList<string>? idiomas = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return CrearResultadoVacio(pagina, tamanioPagina);
        }

        var idiomasNormalizados = NormalizarIdiomas(idiomas);
        var requestBody = new SearchALiciousRequest
        {
            Query = query.Trim(),
            Page = pagina,
            PageSize = tamanioPagina,
            Langs = idiomasNormalizados,
            Fields = SearchFields
        };

        return await EjecutarConMetricasAsync(
            TipoOperacionCatalogoExterno.BusquedaPorTexto,
            async contexto =>
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    new Uri(_options.SearchEndpointUrl),
                    requestBody,
                    cancellationToken);

                contexto.CodigoHttp = (int)response.StatusCode;

                ValidarRespuestaHttp(response, contexto);

                var searchResponse = await response.Content.ReadFromJsonAsync<SearchALiciousResponse>(
                    cancellationToken: cancellationToken);

                if (searchResponse?.Hits == null || searchResponse.Hits.Count == 0)
                {
                    contexto.Resultado = ResultadoLlamadaExterna.NotFound;
                    return CrearResultadoVacio(pagina, tamanioPagina);
                }

                var items = searchResponse.Hits
                    .Where(h => h != null)
                    .Select(MapearHitBusqueda)
                    .ToList();

                if (items.Count == 0)
                {
                    contexto.Resultado = ResultadoLlamadaExterna.NotFound;
                    return CrearResultadoVacio(pagina, tamanioPagina);
                }

                var paginaActual = searchResponse.Page > 0 ? searchResponse.Page : pagina;
                var tamanioActual = searchResponse.PageSize > 0 ? searchResponse.PageSize : tamanioPagina;
                var totalResultados = Math.Max(searchResponse.Count, items.Count);
                var totalPaginas = searchResponse.PageCount > 0
                    ? searchResponse.PageCount
                    : (int)Math.Ceiling((double)totalResultados / tamanioActual);

                contexto.Resultado = ResultadoLlamadaExterna.Success;

                return new ResultadoBusquedaCatalogoExternoDto
                {
                    Items = items,
                    TotalResultados = totalResultados,
                    Pagina = paginaActual,
                    TamanioPagina = tamanioActual,
                    TotalPaginas = totalPaginas
                };
            },
            cancellationToken);
    }

    private async Task<T> EjecutarConMetricasAsync<T>(
        TipoOperacionCatalogoExterno tipoOperacion,
        Func<ContextoLlamadaExterna, Task<T>> operacion,
        CancellationToken cancellationToken)
    {
        var fechaInicioUtc = NormalizarComoUtc(_clock.Now);
        var stopwatch = Stopwatch.StartNew();
        var contexto = new ContextoLlamadaExterna();
        var registrarMetrica = true;

        try
        {
            return await operacion(contexto);
        }
        catch (CatalogoExternoException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            registrarMetrica = false;
            throw;
        }
        catch (TaskCanceledException ex)
        {
            contexto.Resultado = ResultadoLlamadaExterna.Timeout;
            throw new CatalogoExternoException(
                TipoErrorCatalogoExterno.Timeout,
                "La consulta a Open Food Facts excedió el tiempo de espera.",
                ex);
        }
        catch (TimeoutException ex)
        {
            contexto.Resultado = ResultadoLlamadaExterna.Timeout;
            throw new CatalogoExternoException(
                TipoErrorCatalogoExterno.Timeout,
                "La consulta a Open Food Facts excedió el tiempo de espera.",
                ex);
        }
        catch (HttpRequestException ex)
        {
            contexto.Resultado = ResultadoLlamadaExterna.Error;
            throw new CatalogoExternoException(
                TipoErrorCatalogoExterno.ProveedorNoDisponible,
                "No se pudo conectar con el catálogo externo de Open Food Facts.",
                ex);
        }
        catch (JsonException ex)
        {
            contexto.Resultado = ResultadoLlamadaExterna.Error;
            throw new CatalogoExternoException(
                TipoErrorCatalogoExterno.ProveedorNoDisponible,
                "No se pudo procesar la respuesta del catálogo externo de Open Food Facts.",
                ex);
        }
        catch (Exception)
        {
            contexto.Resultado = ResultadoLlamadaExterna.Error;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (registrarMetrica)
            {
                await RegistrarLlamadaAsync(
                    tipoOperacion,
                    fechaInicioUtc,
                    stopwatch.ElapsedMilliseconds,
                    contexto.Resultado,
                    contexto.CodigoHttp);
            }
        }
    }

    private static void ValidarRespuestaHttp(HttpResponseMessage response, ContextoLlamadaExterna contexto)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            contexto.Resultado = ResultadoLlamadaExterna.RateLimited;
            throw new CatalogoExternoException(
                TipoErrorCatalogoExterno.RateLimited,
                "Open Food Facts limitó temporalmente las consultas.");
        }

        if (!response.IsSuccessStatusCode)
        {
            contexto.Resultado = ResultadoLlamadaExterna.Error;
            throw new CatalogoExternoException(
                TipoErrorCatalogoExterno.ProveedorNoDisponible,
                $"Open Food Facts devolvió un error inesperado (HTTP {(int)response.StatusCode}).");
        }
    }

    private static ExternalProductDto MapearProductoBarcode(OpenFoodFactsProductData product, string barcodeFallback)
    {
        var nombreEn = TrimOrNull(product.ProductNameEn) ?? TrimOrNull(product.ProductName);
        var ingredientes = TrimOrNull(product.IngredientsTextEs)
            ?? TrimOrNull(product.IngredientsTextEn)
            ?? TrimOrNull(product.IngredientsText);

        return new ExternalProductDto
        {
            CodigoBarras = TrimOrNull(product.Code) ?? barcodeFallback,
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

    private static ExternalProductDto MapearHitBusqueda(SearchALiciousHit hit)
    {
        var nombreEn = TrimOrNull(hit.ProductNameEn) ?? TrimOrNull(hit.ProductName);
        var ingredientes = TrimOrNull(hit.IngredientsTextEs)
            ?? TrimOrNull(hit.IngredientsTextEn)
            ?? TrimOrNull(hit.IngredientsText);

        return new ExternalProductDto
        {
            CodigoBarras = TrimOrNull(hit.Code),
            Nombre = nombreEn,
            NombreEs = TrimOrNull(hit.ProductNameEs),
            Cantidad = TrimOrNull(hit.Quantity),
            Marca = NormalizarTagsDesdeStringOArray(hit.Brands),
            ImagenUrl = TrimOrNull(hit.ImageFrontUrl) ?? TrimOrNull(hit.ImageUrl),
            Categorias = NormalizarTagsDesdeStringOArray(hit.Categories) ?? NormalizarTags(hit.CategoriesTags),
            Ingredientes = ingredientes,
            Alergenos = NormalizarTags(hit.AllergensTags) ?? NormalizarTagsDesdeStringOArray(hit.Allergens),
            NutriScore = NormalizarNutriScore(hit.NutriScoreGrade),
            Nova = NormalizarNova(hit.NovaGroup)
        };
    }

    private static ResultadoBusquedaCatalogoExternoDto CrearResultadoVacio(int pagina, int tamanioPagina)
    {
        return new ResultadoBusquedaCatalogoExternoDto
        {
            Items = Array.Empty<ExternalProductDto>(),
            TotalResultados = 0,
            Pagina = pagina,
            TamanioPagina = tamanioPagina,
            TotalPaginas = 0
        };
    }

    private static List<string>? NormalizarIdiomas(IReadOnlyList<string>? idiomas)
    {
        if (idiomas == null || idiomas.Count == 0)
        {
            return null;
        }

        var limpios = idiomas
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return limpios.Count == 0 ? null : limpios;
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

    private static int? NormalizarNova(JsonElement? novaGroup)
    {
        if (!novaGroup.HasValue)
        {
            return null;
        }

        var element = novaGroup.Value;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numero))
        {
            return NormalizarNova(numero);
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString()?.Trim(), out var parsed))
        {
            return NormalizarNova(parsed);
        }

        return null;
    }

    private static string? NormalizarTagsDesdeStringOArray(JsonElement? element)
    {
        if (!element.HasValue)
        {
            return null;
        }

        var valor = element.Value;
        if (valor.ValueKind == JsonValueKind.String)
        {
            return NormalizarLista(valor.GetString());
        }

        if (valor.ValueKind == JsonValueKind.Array)
        {
            var tags = valor.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>();

            return NormalizarTags(tags);
        }

        return null;
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

    private sealed class ContextoLlamadaExterna
    {
        public ResultadoLlamadaExterna Resultado { get; set; } = ResultadoLlamadaExterna.Error;
        public int? CodigoHttp { get; set; }
    }

    #region Clases privadas para deserializar el JSON crudo de Open Food Facts
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

    private class SearchALiciousRequest
    {
        [JsonPropertyName("q")]
        public string Query { get; set; } = default!;

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("langs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Langs { get; set; }

        [JsonPropertyName("fields")]
        public string[] Fields { get; set; } = Array.Empty<string>();
    }

    private class SearchALiciousResponse
    {
        [JsonPropertyName("count")]
        public long Count { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("page_count")]
        public int PageCount { get; set; }

        [JsonPropertyName("hits")]
        public List<SearchALiciousHit>? Hits { get; set; }
    }

    private class SearchALiciousHit
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
        public JsonElement? Brands { get; set; }

        [JsonPropertyName("image_front_url")]
        public string? ImageFrontUrl { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("categories")]
        public JsonElement? Categories { get; set; }

        [JsonPropertyName("categories_tags")]
        public List<string>? CategoriesTags { get; set; }

        [JsonPropertyName("ingredients_text_es")]
        public string? IngredientsTextEs { get; set; }

        [JsonPropertyName("ingredients_text_en")]
        public string? IngredientsTextEn { get; set; }

        [JsonPropertyName("ingredients_text")]
        public string? IngredientsText { get; set; }

        [JsonPropertyName("allergens")]
        public JsonElement? Allergens { get; set; }

        [JsonPropertyName("allergens_tags")]
        public List<string>? AllergensTags { get; set; }

        [JsonPropertyName("nutriscore_grade")]
        public string? NutriScoreGrade { get; set; }

        [JsonPropertyName("nova_group")]
        public JsonElement? NovaGroup { get; set; }
    }
    #endregion
}
