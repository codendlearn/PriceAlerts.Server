using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PriceAlerts.Server.Models
{
    public class Product
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("triggerPrice")]
        public decimal? TriggerPrice { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("addedOn")]
        public DateTime AddedOn { get; set; }

        [JsonProperty("modifiedOn")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("minPrice")]
        public decimal MinPrice;

        [JsonProperty("maxPrice")]
        public decimal MaxPrice;

        [JsonProperty("history")]
        public List<ProductHistory> History { get; set; } = new List<ProductHistory>();

        public string ImageUrl { get; set; }
    }
}