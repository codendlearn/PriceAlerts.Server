using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using PriceAlerts.Server.Config;
using Microsoft.Extensions.DependencyInjection;
using HtmlAgilityPack;
using Microsoft.Azure.Cosmos.Fluent;
using System.Net.Http;
using Microsoft.Extensions.Options;
using PriceAlerts.Server.Services;
using PriceAlerts.Server.Repository;
using Microsoft.Azure.Cosmos;

namespace PriceAlerts.Server
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                 .ConfigureAppConfiguration((config) =>
                {
                    var configurationBuilder = config
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((builderContext, serviceCollection) =>
                {
                    serviceCollection.AddHttpClient();
                    serviceCollection.AddLogging();

                    serviceCollection.AddOptions<CosmosDbConfig>()
                        .Configure<IConfiguration>((configSection, configuration) =>
                        {
                            configuration.GetSection(nameof(CosmosDbConfig))
                            .Bind(configSection);
                        });

                    serviceCollection.AddSingleton<CosmosClient>((s) =>
                    {
                        var env = Environment.CurrentDirectory;
                        var configurationBuilder = new CosmosClientBuilder(Environment.GetEnvironmentVariable("CosmosDbConnectionString"));
                        var client = configurationBuilder
                                .WithConnectionModeDirect()
                                .WithHttpClientFactory(() =>
                                {
                                    var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                                    return httpClientFactory.CreateClient();
                                })
                                .Build();

                        // Get values from config.
                        var cosmosDbConfig = new CosmosDbConfig();
                        var section = builderContext.Configuration.GetSection("CosmosDbConfig");
                        section.Bind(cosmosDbConfig);

                        var opt = serviceCollection.BuildServiceProvider().GetRequiredService<IOptions<CosmosDbConfig>>();
                        Console.WriteLine(opt.Value.Database);

                        // Create database and container if not present.
                        var db = client.CreateDatabaseIfNotExistsAsync(cosmosDbConfig.Database).Result;

                        db.Database.DefineContainer(name: cosmosDbConfig.Container, partitionKeyPath: $"/{cosmosDbConfig.PartitionKey}")
                        .WithUniqueKey()
                            .Path("/url")
                        .Attach()
                        .CreateIfNotExistsAsync();

                        return client;
                    });

                    serviceCollection.AddSingleton<HtmlWeb>((c) =>
                    {
                        //     var httpClientFactory = c.GetRequiredService<IHttpClientFactory>();
                        //     var client =  httpClientFactory.CreateClient();

                        var webClient = new HtmlWeb
                        {
                            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.72 Safari/537.36",
                        };

                        return webClient;
                    });

                    serviceCollection.AddScoped<ScrapeService, ScrapeService>();
                    // serviceCollection.AddScoped<EmailService, EmailService>();
                    // serviceCollection.AddScoped<NotificationService, NotificationService>();
                    serviceCollection.AddHttpClient<NotificationService, NotificationService>();
                    serviceCollection.AddScoped<CosmosDbRepository, CosmosDbRepository>();
                })
                .Build();


            Console.WriteLine("Build complete, starting host...");
            host.Run();
        }
    }
}