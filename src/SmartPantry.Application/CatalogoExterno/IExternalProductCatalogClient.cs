using SmartPantry.Productos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartPantry.CatalogoExterno
{
    public interface IExternalProductCatalogClient
    {
        Task<ExternalProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

        Task<ResultadoBusquedaCatalogoExternoDto> SearchByTextAsync(
            string query,
            int pagina = BuscarProductoPorTextoInputDto.PaginaPorDefecto,
            int tamanioPagina = BuscarProductoPorTextoInputDto.TamanioPaginaPorDefecto,
            IReadOnlyList<string>? idiomas = null,
            CancellationToken cancellationToken = default);
    }
}