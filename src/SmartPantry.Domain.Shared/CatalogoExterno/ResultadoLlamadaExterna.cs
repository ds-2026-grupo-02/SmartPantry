namespace SmartPantry.CatalogoExterno;

public enum ResultadoLlamadaExterna
{
    Success = 1,
    NotFound = 2,
    RateLimited = 3,
    Timeout = 4,
    Error = 5
}