using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class EditProduct
    {
        public Product parent { get; set; }
        public List<Product> childs { get; set; }
        public List<string> imgtoShow { get; set; } 
        public List<IFormFile> images { get; set; }
    }
}
