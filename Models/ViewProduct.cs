using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class ViewProduct
    {
        public List<Brand> Brands { get; set; }
        public List<ProductType> ProductTypes { get; set; }
        public List<Product> Products { get; set;}
    }
}
