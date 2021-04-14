using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class OrderDownload
    {
        public string orderNumber { get; set; }
        public DateTime created_at { get; set; }
        public string name { get; set; }
        public string company { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public decimal subtotal_price { get; set; }
        public decimal shipping_price { get; set; }
        public decimal total_tax { get; set; }
        public decimal total_price { get; set; }
        public decimal total_discounts { get; set; }
        public string TipoPago { get; set; }
        public string EstadoPago { get; set; }
        public string status { get; set; }
        public string tipoEnvio { get; set; }
        public string sku { get; set; }
        public string product_id { get; set; }
        public string ProductName { get; set; }
        public int quantity { get; set; }
        public string Link { get; set; }
        public decimal Price { get; set; }
        public string Cliente { get; set; }
        public string SFacturacion { get; set; }
        public string SReferencia { get; set; }
        public string SDni { get; set; }
        public string SCity { get; set; }
        public string SCountry { get; set; }
        public string SProvince { get; set; }
        public string SPhone { get; set; }
        public string Destinatario { get; set; }
        public string DEntrega { get; set; }
        public string DReferencia { get; set; }
        public string DDni { get; set; }
        public string DCity { get; set; }
        public string DCountry   { get; set; }
        public string DProvince { get; set; }
        public string DPhone { get; set; }
        public string ToLine()
        {
            return $"{orderNumber};{created_at.ToString("dd/MM/yyyy HH:mm")};{name};{company};{email};{phone};{subtotal_price};{shipping_price};{total_tax};{total_price};{total_discounts};{TipoPago};{EstadoPago};{status};{tipoEnvio};{sku};{product_id};{quantity};{ProductName};{Link};{Price};{Cliente};{SFacturacion};{SReferencia};{SDni};{SCity};{SCountry};{SProvince};{SPhone};{Destinatario};{DEntrega};{DReferencia};{DDni};{DCity};{DCountry};{DProvince};{DPhone};";
        }
    }
}
