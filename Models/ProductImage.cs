using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class ProductImage
    {
        [Key]
        public string id { get; set; }
        public string product_id { get; set; }
        public string alt { get; set; }
        public string src { get; set; }
        public int position { get; set; }
    }
}
