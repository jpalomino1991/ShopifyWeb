using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class OrderList
    {
        public string id { get; set; }
        public string numero { get; set; }
        public DateTime fecha { get; set; }
        public string nombre { get; set; }
        public decimal total { get; set; }
        public string tipo { get; set; }
        public string estado { get; set; }
        public string estadoOrden { get; set; }
        public string dni { get; set; }
        public string correo { get; set; }
        public string telefono { get; set; }
    }
}
