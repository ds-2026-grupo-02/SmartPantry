using SmartPantry.Productos;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace SmartPantry.Productos;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private readonly HttpClient _httpClient;

    // Inyectamos el HttpClient que será configurado mediante IHttpClientFactory
    public OpenFoodFactsProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        // Endpoint v3 de Open Food Facts para buscar por código de barras
        const string fields = "code,product_name,product_name_es,brands,image_front_url";
        var requestUri = $"product/{Uri.EscapeDataString(barcode)}?fields={Uri.EscapeDataString(fields)}";

        try
        {
            var response = await _httpClient.GetAsync(requestUri);

            // Si el producto no existe (404)
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new UserFriendlyException("Open Food Facts limitó temporalmente las consultas. Por favor, intente más tarde.");
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new UserFriendlyException($"Open Food Facts devolvió un error inesperado (HTTP {(int)response.StatusCode}).");
            }

            // Deserializa la respuesta cruda de la API externa
            var offResponse = await response.Content.ReadFromJsonAsync<OpenFoodFactsApiResponse>();

            // Si el estado del proveedor indica que no se encontró el producto
            if (offResponse == null || offResponse.Status != "success" || offResponse.Product == null)
            {
                return null;
            }

            // Mapeamos de la respuesta cruda al ExternalProductDto propio (RF-09: traduciendo sin inventar)
            return new ExternalProductDto
            {
                Nombre = string.IsNullOrWhiteSpace(offResponse.Product.ProductName) ? null : offResponse.Product.ProductName.Trim(),
                NombreEs = string.IsNullOrWhiteSpace(offResponse.Product.ProductNameEs) ? null : offResponse.Product.ProductNameEs.Trim(),
                Marca = string.IsNullOrWhiteSpace(offResponse.Product.Brands) ? null : offResponse.Product.Brands.Trim(),
                ImagenUrl = string.IsNullOrWhiteSpace(offResponse.Product.ImageFrontUrl) ? null : offResponse.Product.ImageFrontUrl.Trim()
            };
        }
        catch (TaskCanceledException)
        {
            throw new UserFriendlyException("La consulta a Open Food Facts excedió el tiempo de espera.");
        }
        catch (HttpRequestException)
        {
            throw new UserFriendlyException("No se pudo conectar con el catálogo externo de Open Food Facts.");
        }
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
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("product_name_es")]
        public string? ProductNameEs { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("image_front_url")]
        public string? ImageFrontUrl { get; set; }
    }
    #endregion
}