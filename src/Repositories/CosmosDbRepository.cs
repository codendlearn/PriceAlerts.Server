using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PriceAlerts.Server.Config;
using PriceAlerts.Server.Models;

namespace PriceAlerts.Server.Repository
{
    public class CosmosDbRepository
    {
        private readonly ILogger logger;
        private readonly CosmosClient cosmosClient;
        private readonly CosmosDbConfig config;
        private Database productDatabase;
        private Container productContainer;

        public CosmosDbRepository(ILogger<CosmosDbRepository> logger,
            CosmosClient cosmosClient,
            IOptions<CosmosDbConfig> config
            )
        {
            this.logger = logger;
            this.cosmosClient = cosmosClient;
            this.config = config.Value;
            productDatabase = this.cosmosClient.GetDatabase(this.config.Database);
            productContainer = productDatabase.GetContainer(this.config.Container);
        }

        public async Task<ConcurrentBag<Product>> GetAllProducts()
        {
            QueryDefinition query = new QueryDefinition($"SELECT * FROM {productContainer.Id} c where c.isActive=@isActive").WithParameter("@isActive", true);
            FeedIterator<Product> resultSet = productContainer.GetItemQueryIterator<Product>(
                   query,
                   requestOptions: new QueryRequestOptions()
                   {
                       MaxItemCount = 10
                   });

            var items = new ConcurrentBag<Product>();
            while (resultSet.HasMoreResults)
            {
                logger.LogInformation("Retreving all contacts");
                try
                {
                    FeedResponse<Product> response = await resultSet.ReadNextAsync();
                    logger.LogInformation("asfd {response}", response);
                    Parallel.ForEach(response, (p) =>
                    {
                        items.Add(p);
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError("Error: {ex}", ex);
                }
            }

            return items;
        }

        internal async Task<IEnumerable<Product>> GetAllActiveProductsAsync()
        {
            QueryDefinition query = new QueryDefinition($"SELECT * FROM c where c.isActive=@isActive")
            .WithParameter("@isActive", true);

            FeedIterator<Product> resultSet = productContainer.GetItemQueryIterator<Product>(
                   query,
                   requestOptions: new QueryRequestOptions()
                   {
                       MaxItemCount = 10
                   });

            var items = new List<Product>();
            while (resultSet.HasMoreResults)
            {
                try
                {
                    FeedResponse<Product> response = await resultSet.ReadNextAsync();
                    items.AddRange(response);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching active products, continuing...");
                }
            }

            return items;
        }

        internal async Task AddAsync(Product product)
        {
            try
            {
                await productContainer.CreateItemAsync(product, new PartitionKey(product.Category));
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, $"Error adding product: {JsonConvert.SerializeObject(product)}");
                throw;
            }
        }

        internal async Task<Product> DeleteAsync(string category, string productId)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentNullException("Category or productId is null");
            }

            try
            {
                logger.LogInformation($"Deleting product with {category} and {productId}", category, productId);
                var res = await productContainer.DeleteItemAsync<Product>(productId, new PartitionKey(category));
                logger.LogInformation($"Deleting product with {category} and {productId}: {res}", category, productId, res);
                return res.Resource;
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Unable to delete Product with {category} and {productId}", category, productId);
                throw;
            }
        }

        internal async Task<bool> UpdateProductAsync(Product product, string id)
        {
            if (product == null || string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("Product details are not valid.");
            }

            var res = await productContainer.ReplaceItemAsync<Product>(product, id);
            return true;
        }
    }
}