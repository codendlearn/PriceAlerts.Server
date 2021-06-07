using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PriceAlerts.Server.Models;
using PriceAlerts.Server.Repository;
using PriceAlerts.Server.Services;

namespace PriceAlerts.Server.Functions
{
    public class ProductsController
    {
        private readonly CosmosDbRepository cosmosRepository;
        private readonly ILogger<ProductsController> logger;
        private readonly ScrapeService scrapeService;

        public ProductsController(CosmosDbRepository cosmosDbRepository,
        ILogger<ProductsController> logger,
        ScrapeService scrapeService)
        {
            this.cosmosRepository = cosmosDbRepository;
            this.logger = logger;
            this.scrapeService = scrapeService;
        }

        [Function("AllProducts")]
        public async Task<HttpResponseData> GetAllProducts([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var products = await cosmosRepository.GetAllProducts();
            logger.LogTrace($"TReceived {products.Count} Products.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(products);

            return response;
        }

        [Function("AddProduct")]
        public async Task<HttpResponseData> AddProduct([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
           FunctionContext executionContext)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            IEnumerable<Product> productsToAdd;
            var errorProducts = new List<Product>();
            var token = JToken.Parse(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);

            if (token is JArray)
            {
                productsToAdd = token.ToObject<List<Product>>();
            }
            else if (token is JObject)
            {
                productsToAdd = new List<Product> { token.ToObject<Product>() };
            }
            else
            {   
                response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync("Please provide valid details.");
                return response;
            }

            var products = new List<Product>();
            foreach (var product in productsToAdd)
            {
                if(string.IsNullOrWhiteSpace(product.Category))
                {
                    product.Category = "UnKnown";
                } 

                try
                {
                    var tProduct = await scrapeService.GetProduct(product.Url);
                    tProduct.Title = string.IsNullOrWhiteSpace(product.Title) ? tProduct.Title : product.Title;
                    tProduct.Category =  tProduct.Category ?? product.Category;
                    tProduct.TriggerPrice = product.Price;
                    tProduct.Id = Guid.NewGuid().ToString();
                    tProduct.AddedOn = DateTime.UtcNow;
                    tProduct.IsActive = true;

                    logger.LogInformation("Product {productToAdd}", product);

                    await cosmosRepository.AddAsync(tProduct);
                }
                catch (CosmosException ex)
                {
                    logger.LogError(ex, "Error inserting Product  {productsToAdd}", productsToAdd);
                    errorProducts.Add(product);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "unhandled Error inserting Product  {productsToAdd}", productsToAdd);
                    errorProducts.Add(product);
                }
            }

            if (errorProducts.Count > 0)
            {
                response = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
                await response.WriteAsJsonAsync(new { errorMessage = "Error adding one or more products", FailedProducts = errorProducts, SucceededProducts = productsToAdd.Where(p => !errorProducts.Any(e => e.Url == p.Url)) });
                return response;
            }

            return response;
        }

        [Function("DeleteProduct")]
        public async Task<HttpResponseData> FunctionDeleteProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "delete/{category}/{productId}")] HttpRequestData req,
            ILogger log,
            string productId,
            string category)
        {
            HttpResponseData response = req.CreateResponse(HttpStatusCode.BadRequest);
            if (string.IsNullOrWhiteSpace(productId))
            {
                await response.WriteStringAsync("Invalid productid passed");
                return response;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                await response.WriteStringAsync("Invalid category passed");
                return response;
            }

            var result = await cosmosRepository.DeleteAsync(category, productId);
            response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
