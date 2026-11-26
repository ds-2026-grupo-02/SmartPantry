using System;

namespace SmartPantry.Despensa;

public class NotaPrivadaDto
{
    public Guid Id { get; set; }
    public Guid ItemDespensaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
}


//para devolver la nota al consultarla o guardarla