using System.Collections.Generic;

namespace ShopifyWeb.Models
{
    public class Fulfillment
    {
        public string id { get; set; }
        public string location_id { get; set; }
        public string tracking_number { get; set; }
        public List<LineItem> line_items { get; set; }
    }
}