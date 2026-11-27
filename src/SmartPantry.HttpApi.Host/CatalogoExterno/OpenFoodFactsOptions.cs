namespace SmartPantry.CatalogoExterno;

public class OpenFoodFactsOptions
{
    public const string SectionName = "OpenFoodFacts";

    public string ProductApiBaseUrl { get; set; } = "https://world.openfoodfacts.org/api/v3/";
    public string SearchEndpointUrl { get; set; } = "https://search.openfoodfacts.org/search";
    public int TimeoutSeconds { get; set; } = 15;
    public string UserAgent { get; set; } = "SmartPantryUTN/1.0 (+https://github.com/ds-2026-grupo-02/SmartPantry)";
}