using Newtonsoft.Json;

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
