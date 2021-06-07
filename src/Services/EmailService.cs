// using System;
// using System.Collections.Concurrent;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.Azure.WebJobs;
// using Microsoft.Extensions.Logging;
// using PriceAlerts.Server.Models;
// using SendGrid.Helpers.Mail;

// namespace PriceAlerts.Server.Services
// {
//     public class EmailService
//     {
//         private readonly ILogger<EmailService> logger;

//         public EmailService(ILogger<EmailService> logger)
//         {
//             this.logger = logger;
//         }

//         public async Task SendEmail(IAsyncCollector<SendGridMessage> messageCollector, ConcurrentDictionary<string, double> products)
//         {
//             var body = products.Select(x => $@"{x.Key}\t{x.Value}\n");
//             var value = string.Join('\n', body.ToArray());
//             var email = new Email
//             {
//                 From = "codinesh@live.com",
//                 To = "codinesh@live.com;nandini.narapureddy@gmail.com",
//                 Body = $@"Hello, there is change in price for below items.\n {value}",
//                 Subject = $"Price Change Alert for {products.Count} items"
//             };

//             var message = new SendGridMessage();
//             message.AddTo(email.To);
//             message.AddContent("text/html", email.Body);
//             message.SetFrom(new EmailAddress(email.From));
//             message.SetSubject(email.Subject);

//             try
//             {
//                 await messageCollector.AddAsync(message);
//             }
//             catch (Exception ex)
//             {
//                 logger.LogError(ex, "Error Sending email");
//             }
//         }

//     }
// }