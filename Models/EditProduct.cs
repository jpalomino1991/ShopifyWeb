using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class EditProduct
    {
        public List<Product> productDetail { get; set; }
        public List<IFormFile> images { get; set; }
    }
}
