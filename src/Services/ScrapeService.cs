using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PriceAlerts.Server.Extensions;
using PriceAlerts.Server.Models;

namespace PriceAlerts.Server.Services
{
    public class ScrapeService
    {
        private readonly HtmlWeb webClient;
        private readonly ILogger<ScrapeService> logger;

        public ScrapeService(HtmlWeb webClient, ILogger<ScrapeService> logger)
        {
            this.webClient = webClient;
            this.logger = logger;
        }

        internal async Task<Product> GetProduct(string url)
        {
            var product = new Product
            {
                Url = url
            };

            var doc = await webClient.LoadFromWebAsync(url);
            product.Price = doc.DocumentNode.GetProductPrice("priceblock_ourprice", "priceblock_dealprice");
            product.Title = doc.DocumentNode.GetValue("productTitle");
            product.Category = doc.DocumentNode.GetChildrenText("wayfinding-breadcrumbs_feature_div", "a");
            product.ImageUrl = doc.DocumentNode.GetProductImage();
            return product;
        }

        public async Task<decimal> GetProductPrice(string url)
        {
            var doc = await webClient.LoadFromWebAsync(url);
            var productPrice = doc.DocumentNode.GetProductPrice("priceblock_ourprice", "priceblock_dealprice");
            return productPrice;
        }

        public async Task<string> GetProductImageurl(string url)
        {
            try
            {
                var doc = await webClient.LoadFromWebAsync(url);
                var imageurl = doc.DocumentNode.GetProductImage();
                return imageurl;
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Error fetching product url.");
                throw ex;
            }
        }

    }
}