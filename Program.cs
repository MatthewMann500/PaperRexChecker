using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Collections.Generic;

class Program
{
    static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        string url = "https://shop.pprx.team/collections/paper-rex-official"; // First Page
        int checkIntervalSeconds = 600; // Ten Minutes

        Console.WriteLine($"Watching {url} for stock changes...");

        // Holds product name -> whether it's in stock from the last check
        Dictionary<string, bool> previousStockStatus = new Dictionary<string, bool>();

        while (true)
        {
            try
            {
                // Fetch HTML from the PRX store
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Select all product cards
                var productCards = doc.DocumentNode.SelectNodes("//div[contains(@class, 'card') and contains(@class, 'card--product') and contains(@class, 'card--traditional')]");
                Dictionary<string, bool> currentStatus = new Dictionary<string, bool>();

                Console.WriteLine($"\n{DateTime.Now} — Current Stock Status:");

                if (productCards != null)
                {
                    foreach (var card in productCards)
                    {
                        // Extract product title
                        var titleNode = card.SelectSingleNode(".//div[contains(@class, 'card-body')]//h3[contains(@class, 'card-title')]//a");
                        string name = titleNode?.InnerText.Trim() ?? "Unnamed Product";


                        // Check if the product has the 'Sold Out' badge
                        var soldOutBadge = card.SelectSingleNode(".//a[contains(@class, 'card-media')]//div[contains(@class, 'card-media-overlay')]//div");
                        bool isInStock = soldOutBadge == null;

                        // Save current stock status
                        currentStatus[name] = isInStock;

                        // Print current status of each product (always)
                        Console.WriteLine($"   • {name}: {(isInStock ? "In Stock" : "Out of Stock")}");

                        // If we've seen this product before, compare to previous status
                        if (previousStockStatus.ContainsKey(name))
                        {
                            bool wasInStock = previousStockStatus[name];

                            // Detect transitions (in stock or out of stock)
                            if (!wasInStock && isInStock)
                            {
                                // Product just came in stock
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"'{name}' just came IN STOCK!");
                                Console.ResetColor();

                                // TODO: Add notification logic here (SMS, push, etc.)
                            }
                            else if (wasInStock && !isInStock)
                            {
                                // Product just went out of stock
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"'{name}' just went OUT OF STOCK.");
                                Console.ResetColor();

                                // TODO: Add notification logic here (SMS, push, etc.)
                            }
                        }
                        else
                        {
                            // First time seeing this product (new)
                            Console.WriteLine($"New product detected: {name} — currently {(isInStock ? "In Stock" : "Out of Stock")}");
                        }
                    }
                }

                // Save current state for next loop comparison
                previousStockStatus = currentStatus;
            }
            catch (Exception ex)
            {
                // Handle network or parsing errors
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }

            // Wait for next check
            await Task.Delay(TimeSpan.FromSeconds(checkIntervalSeconds));
        }
    }
}
