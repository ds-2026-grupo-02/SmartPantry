using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using SmartPantry.Productos;

namespace SmartPantry.Productos;

// TransientDependency le indica a ABP que registre automáticamente esta clase en el contenedor DI
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
        var requestUri = $"api/v3/product/{barcode}.json?fields=product_name,brands,image_url";

        try
        {
            var response = await _httpClient.GetAsync(requestUri);

            // Si el producto no existe (404)
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            // Asegura que no haya fallas de red / servidor HTTP
            response.EnsureSuccessStatusCode();

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
                Marca = string.IsNullOrWhiteSpace(offResponse.Product.Brands) ? null : offResponse.Product.Brands.Trim(),
                ImagenUrl = string.IsNullOrWhiteSpace(offResponse.Product.ImageUrl) ? null : offResponse.Product.ImageUrl.Trim()
            };
        }
        catch (HttpRequestException)
        {
            // En caso de fallas de red o límites de uso de la API externa
            throw;
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

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
    #endregion
}