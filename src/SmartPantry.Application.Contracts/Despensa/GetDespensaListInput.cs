using Volo.Abp.Application.Dtos;

namespace SmartPantry.Despensa;


//DTO para filtrar el listado (activos vs. consumidos)
public class GetDespensaListInput : PagedAndSortedResultRequestDto
{
    public bool? SoloConsumidos { get; set; }
}

//Si SoloConsumidos es false o null, pediremos los ítems activos (!EsConsumido).
//Si SoloConsumidos es true, pediremos el historial de consumidos(EsConsumido).