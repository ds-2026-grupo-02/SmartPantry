using SmartPantry.Productos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SmartPantry.CatalogoExterno
{
    public interface IExternalProductCatalogClient
    {
        Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
    }
}
