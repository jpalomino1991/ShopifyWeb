using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class ProductDownload
    {
        public string SKU { get; set; }
        public string Handle { get; set; }
        public string Title { get; set; }
        public string Tags { get; set; }
        public string Id { get; set; }
        public string ProductType { get; set; }
        public string Vendor { get; set; }
        public string SEOTitle { get; set; }
        public string SEODescription { get; set; }
        public string URL { get; set; }
        public string Talla { get; set; }
        public int Stock { get; set; }
        public decimal PrecioTV { get; set; }
        public decimal PrecioPromocion { get; set; }
        public string Color { get; set; }
        public string Material { get; set; }
        public string Taco { get; set; }
        public string Genero { get; set; }
        public string Ocasion { get; set; }
        public string Tendencia { get; set; }
        public string Departamento { get; set; }
        public string Status { get; set; }
        public int CantidadImagenes { get; set; }
        public DateTime CreateDate { get; set; }
        public string Marca { get; set; }
        public string MaterialInterior { get; set; }
        public string MaterialSuela { get; set; }
        public string HechoEn { get; set; }
        public string CodigoProducto { get; set; }
        public int Peso { get; set; }
        public string ToLine()
        {
            return $"{SKU};{Handle};{Title};{Tags};{Id};{ProductType};{Vendor};{SEOTitle};{SEODescription};{URL};{Talla};{Stock};{PrecioTV};{PrecioPromocion};{Color};{Material};{Taco};{Genero};{Ocasion};{Tendencia};{Departamento};{Status};{CantidadImagenes};{CreateDate};{Marca};{MaterialInterior};{MaterialSuela};{HechoEn};{CodigoProducto};{Peso};";
        }
    }
}
