namespace PriceAlerts.Server.Config
{
    public class CosmosDbConfig
    {
        public string Database { get; set; }
        public string Container { get; set; }
        public string PartitionKey { get; set; }
    }
}