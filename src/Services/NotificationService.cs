using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PriceAlerts.Server.Models;

namespace PriceAlerts.Server.Services
{
    public class NotificationService
    {
        private readonly HttpClient client;
        private readonly ILogger<NotificationService> logger;

        public NotificationService(HttpClient client, ILogger<NotificationService> logger)
        {
            this.client = client;
            this.logger = logger;
        }
        public async Task SendNotification(Product product, ProductHistory history)
        {
            var telegramChannelUrl = "https://api.telegram.org/bot1789590888:AAHSF1kYvTJSGXL6UC5CMwCnD675E7NrdO8/";
            var sendMessageEndpoint = "sendMessage";

            var changeType = history.PreviousPrice < history.CurrentPrice ? "increased" : "decreased";
            var title = product.Title.Length > 50 ? product.Title.Substring(0, 50) + "..." : product.Title;
            var message = new
            {
                chat_id = "-1001412627466",
                text = $"*Price {changeType} for [{title}]({product.Url})*\n\nPrevious Price: {history.PreviousPrice}\n__Current Price: {history.CurrentPrice}__\n\n at {DateTime.UtcNow.ToLocalTime()}",
                parse_mode = "MarkdownV2"
            };

            try
            {
                logger.LogInformation("Sending notification to telegram, with message {messge}", message);
                await client.PostAsJsonAsync($"{telegramChannelUrl}{sendMessageEndpoint}", message);
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Error sending notification to telegram.");
            }
        }
    }
}