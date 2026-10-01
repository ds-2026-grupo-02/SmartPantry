using System;
using Volo.Abp.Application.Dtos;


namespace SmartPantry.Productos
{
    public class ExternalProductDto
    {
        public string? Nombre { get; set; }
        public string? NombreEs { get; set; }
        public string? Marca { get; set; }
        public string? ImagenUrl { get; set; }
    }
}
