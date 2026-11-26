using System;

namespace SmartPantry.Advertencias;

public class UmbralVencimiento
{
    public const int DiasPorDefecto = 3;  //defini un valor estandar, no es por nada en particular

    public int Dias { get; }

    public UmbralVencimiento(int dias = DiasPorDefecto)
    {
        if (dias < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dias), "El umbral de días no puede ser negativo.");
        }

        Dias = dias;
    }

    public static UmbralVencimiento PorDefecto() => new(DiasPorDefecto);     //Crea un umbral utilizando los 3 días predeterminados.
}