using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace PriceAlerts.Server.Extensions
{
    public static class ScrapeHelperExtensions
    {
        public static string GetChildrenText(this HtmlNode node, string parentId, string child)
        {
            var value = string.Empty;
            var categoryNode = node.SelectSingleNode($"//div[@id='{parentId}']//{child}");
            if (categoryNode != null)
            {
                value = categoryNode.InnerText?.Trim() ?? string.Empty;
            }

            return value;
        }

        public static string GetProductImage(this HtmlNode node)
        {
            var value = string.Empty;
            var categoryNode = node.SelectSingleNode($"//*[@id='landingImage']");
            if (categoryNode != null)
            {
                value = categoryNode.GetAttributeValue("src", string.Empty);
            }

            return value;
        }

        public static decimal GetProductPrice(this HtmlNode node, params string[] ids)
        {
            var result = node.GetValue(ids);
            if (result == null || string.IsNullOrWhiteSpace(result))
            {
                return 0;
            }

            var regex = "[^0-9,.]";
            var priceString = Regex.Replace(result, regex, string.Empty);
            if (decimal.TryParse(priceString, out var price))
            {
                return price;
            }

            return 0;
        }

        public static string GetValue(this HtmlNode node, params string[] ids)
        {
            var result = string.Empty;
            foreach (var id in ids)
            {
                var priceNode = node.SelectSingleNode($"//span[@id='{id}']");
                if (priceNode == null)
                {
                    continue;
                }
                else
                {
                    result = priceNode.InnerText?.Trim();
                    break;
                }
            }

            return result;
        }
    }
}