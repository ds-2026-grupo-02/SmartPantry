namespace SmartPantry;

public static class SmartPantryDomainErrorCodes
{
    /* You can add your business exception error codes here, as constants */
    public const string PreferenciaNombreVacio = "SmartPantry:00001";
    public const string PreferenciaNombreLargo = "SmartPantry:00002";
    public const string PreferenciaDuplicado = "SmartPantry:00003";
    public const string PreferenciaDemasiadosItems = "SmartPantry:00004";

    public const string ProductoCodigoBarrasDuplicado = "SmartPantry:ProductoCodigoBarrasDuplicado";
    public const string CatalogoExternoRateLimit = "SmartPantry:CatalogoExternoRateLimit";
    public const string CatalogoExternoTimeout = "SmartPantry:CatalogoExternoTimeout";
    public const string CatalogoExternoNoDisponible = "SmartPantry:CatalogoExternoNoDisponible";
}
