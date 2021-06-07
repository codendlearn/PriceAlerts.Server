using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PriceAlerts.Server.Models
{
    public class ProductHistory
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("currentprice")]
        public decimal CurrentPrice { get; set; }

        [JsonProperty("previousprice")]
        public decimal PreviousPrice { get; set; }

        [JsonProperty("updatedOn")]
        public DateTime UpdatedOn { get; set; }

        [JsonProperty("notified")]
        public bool Notified { get; set; }
    }
}