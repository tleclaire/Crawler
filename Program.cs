using Crawler.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    class Program
    {
        static void Main(string[] args)
        {

            string siteUrl = String.Empty;

            for (int i = 0; i <= args.GetUpperBound(0); i++)
            {
                switch (args[i].ToUpper())
                {
                    case "-URL":
                        siteUrl = args[i + 1];
                        i++;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unbekannter Parameter" + args[i]);
                        Console.WriteLine("Verwendung : Crawler -Url https://www.siteurl.com");
                        return;
                }
            }

            SiteCrawler siteCrawler = new SiteCrawler(siteUrl);
            siteCrawler.OnPageLoaded += SiteCrawler_OnPageLoaded;
            siteCrawler.OnErrorLoadingPage += SiteCrawler_OnErrorLoadingPage;
            siteCrawler.StartCrawling();
        }


        private static void SiteCrawler_OnPageLoaded(string title, string content)
        {
            Console.WriteLine("Saving " + title);

            var fileName = $"output\\{title}";
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            if (!string.IsNullOrEmpty(fileName))
                File.WriteAllText($"{fileName}.html",content);

        }
        private static void SiteCrawler_OnErrorLoadingPage(string address, Exception exception)
        {
            Console.WriteLine($"Error loading {address}, Exception {exception.Message}");
        }
    }
}
