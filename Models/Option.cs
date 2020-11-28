using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShopifyWeb.Models
{
    public class Option
    {
        public string name { get; set; }
        public int position { get; set; }
        [JsonIgnore]
        public string values { get; set; }
    }
}
