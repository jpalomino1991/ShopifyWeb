﻿using System.ComponentModel.DataAnnotations;

namespace ShopifyWeb.Models
{
    public class Web
    {
        [Key]
        public int Id { get; set; }
        public string WebURL { get; set; }
        public string WebAPI { get; set; }
        public string WebPassword { get; set; }
        public string LocationId { get; set; }
    }
}