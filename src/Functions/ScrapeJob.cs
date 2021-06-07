using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PriceAlerts.Server.Models;
using PriceAlerts.Server.Repository;
using PriceAlerts.Server.Services;

namespace PriceAlerts.Server.Functions
{
    public class ScrapeJob
    {
        private readonly ScrapeService scrapeService;
        private readonly CosmosDbRepository cosmosRepository;
        private readonly NotificationService notificationService;
        private readonly ILogger<ScrapeJob> logger;

        public ScrapeJob(ScrapeService scrapeService,
            CosmosDbRepository cosmosDbRepository,
            NotificationService notificationService,
            ILogger<ScrapeJob> logger)
        {
            this.scrapeService = scrapeService;
            this.cosmosRepository = cosmosDbRepository;
            this.notificationService = notificationService;
            this.logger = logger;
        }

        [Function("ScrapeJob")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] MyInfo myTimer)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");;
            logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");

            var products = await cosmosRepository.GetAllActiveProductsAsync();
            var items = new ConcurrentDictionary<string, decimal>();
            foreach (var product in products)
            {
                logger.LogInformation($"Fetching {product.Title}");
                if (Uri.IsWellFormedUriString(product.Url, UriKind.Absolute))
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(product.ImageUrl))
                        {
                            product.ImageUrl = await scrapeService.GetProductImageurl(product.Url);
                        }

                        var price = await scrapeService.GetProductPrice(product.Url);
                        if (price != 0)
                        {
                            items.TryAdd(product.Url, price);
                            logger.LogInformation($"{product.Url} => {price}");

                            if (price != product.Price)
                            {
                                var curTime = DateTime.UtcNow;
                                var history = new ProductHistory
                                {
                                    CurrentPrice = price,
                                    Notified = false,
                                    PreviousPrice = product.Price,
                                    UpdatedOn = curTime
                                };

                                if (price < product.Price)
                                {
                                    product.MinPrice = price;
                                    logger.LogInformation($"Price for {product.Title} reduced to {price}.");
                                }
                                else
                                {
                                    product.MaxPrice = price;
                                    logger.LogInformation($"Price for {product.Title} increased to {price}.");
                                }

                                product.Price = price;
                                product.History.Add(history);
                                product.ModifiedOn = curTime;

                                logger.LogInformation("Sending telegram notification");
                                await notificationService.SendNotification(product, history);

                                var replaceContact = await cosmosRepository.UpdateProductAsync(product, product.Id);
                                logger.LogWarning($"Updated {product.Title}");
                            }
                            else
                            {
                                logger.LogWarning($"No price differnece for {product.Title}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error: {ex}", ex);
                    }
                }
                else
                {
                    logger.LogError($"Can't find price for {product.Url}");
                }
            }
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
