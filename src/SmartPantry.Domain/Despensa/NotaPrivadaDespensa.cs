using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Despensa;

public class NotaPrivadaDespensa : FullAuditedEntity<Guid>
{
    public const int MaxContenidoLength = 500;

    public Guid ItemDespensaId { get; private set; } //referencia al ítem de despensa al que pertenece la nota.
    public Guid UsuarioId { get; private set; }  // el ID del usuario propietario de la nota (para reforzar la privacidad).
    public string Contenido { get; private set; } = string.Empty;   //el texto de la nota, recortado con .Trim(), obligatorio (no vacío/espacios)
                                                                    /// <summary>
                                                                    //y validado para que no supere un máximo (por ejemplo, 500 caracteres).
                                                                    /// </summary>

    protected NotaPrivadaDespensa() { }

    public NotaPrivadaDespensa(
        Guid id,
        Guid itemDespensaId,
        Guid usuarioId,
        string contenido)
        : base(id)
    {
        ItemDespensaId = itemDespensaId;
        UsuarioId = usuarioId;
        ActualizarContenido(contenido);
    }

    public void ActualizarContenido(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            throw new ArgumentException("La nota privada no puede estar vacía.", nameof(contenido));
        }

        var limpio = contenido.Trim();

        if (limpio.Length > MaxContenidoLength)
        {
            throw new ArgumentException(
                $"La nota privada no puede superar los {MaxContenidoLength} caracteres.",
                nameof(contenido));
        }

        Contenido = limpio;
    }
}