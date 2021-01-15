using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class ImageShopify
    {
        public string id { get; set; }
        public string product_id { get; set; }
        public string attachment { get; set; }
        public string filename { get; set; }
        public string alt { get; set; }
        public string src { get; set; }
        public int position { get; set; }
    }
}
