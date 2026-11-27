using System;

namespace SmartPantry.CatalogoExterno;

public class CatalogoExternoException : Exception
{
    public TipoErrorCatalogoExterno TipoError { get; }

    public CatalogoExternoException(
        TipoErrorCatalogoExterno tipoError,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        TipoError = tipoError;
    }
}
