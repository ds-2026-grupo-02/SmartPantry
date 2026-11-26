using Volo.Abp;

namespace SmartPantry.Preferencias;

internal static class NombreVigilado
{
    public static string Normalizar(string? valor)
    {
        var normalizado = valor?.Trim();

        if (string.IsNullOrEmpty(normalizado))
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.PreferenciaNombreVacio);
        }

        if (normalizado.Length > PreferenciaUsuarioConsts.MaxNombreLength)
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.PreferenciaNombreLargo)
                .WithData("Maximo", PreferenciaUsuarioConsts.MaxNombreLength);
        }

        return normalizado;
    }
}
