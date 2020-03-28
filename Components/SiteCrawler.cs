using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Crawler.Components
{
    /// <summary>
    /// Component that crawls a web site from the start page and raises event to consumer on every scanned page
    /// </summary>
    public class SiteCrawler
    {
        public delegate void PageLoaded(string title,string content);
        public event PageLoaded OnPageLoaded;

        public delegate void ErrorLoadingPage(string address, Exception exception);
        public event ErrorLoadingPage OnErrorLoadingPage;

        string startAddress;
        public SiteCrawler(string startUrl)
        {
            startAddress = startUrl;
        }

        public void StartCrawling()
        {
            List<string> scannedPages = new List<string>();
            CrawlSite(startAddress, startAddress, ref scannedPages);
        }
        private void CrawlSite(string baseUrl, string startUrl, ref List<string> scannedPages)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDocument = web.Load(startUrl);
            if (web.ResponseUri.AbsoluteUri.StartsWith(baseUrl))
            {
                List<HtmlNode> tags = new List<HtmlNode>();
                GetTagsByName(ref tags, htmlDocument.DocumentNode.ChildNodes, Consts.TagNames.Anchor);

                foreach (var tag in tags)
                {
                    if (CheckTag(tag,baseUrl))
                    {
                        string page;
                        if (tag.Attributes[Consts.AttributeNames.Href].Value.StartsWith("http") && !tag.Attributes[Consts.AttributeNames.Href].Value.StartsWith(baseUrl))//make sure to scann only withinh the domain of the crawled site
                            continue;

                        page = BuildPageToCrawlUrl(baseUrl, tag);
                        if (!scannedPages.Contains(page))
                        {
                            //due to haveing the same Urls on every page, save the address in a list and check if it's been scanned already to prevent endless looping
                            scannedPages.Add(page);
                            try
                            {
                                htmlDocument = web.Load(page);
                                var title = RemoveInvalidCharacters(htmlDocument.DocumentNode.SelectSingleNode("//title")?.InnerText); //Get Title of page and clean it 
                                if (OnPageLoaded != null)
                                {
                                    OnPageLoaded(title, htmlDocument.DocumentNode.InnerHtml); //Raise Event to consumer to let it do what ever it wants.
                                }
                                CrawlSite(baseUrl, page, ref scannedPages); //Recursively call the crawler for every page
                            }
                            catch (Exception ex) //Ignore unknown errors
                            {
                                if(OnErrorLoadingPage!= null)
                                {
                                    OnErrorLoadingPage(tag.Attributes[Consts.AttributeNames.Href].Value, ex);
                                }
                            }
                        }
                    }

                }
            }

        }

        private static string BuildPageToCrawlUrl(string baseUrl, HtmlNode tag)
        {
            string page;
            if (tag.Attributes[Consts.AttributeNames.Href].Value.StartsWith("http"))
                page = $"{tag.Attributes[Consts.AttributeNames.Href].Value}";
            else
                page = $"{baseUrl}{tag.Attributes[Consts.AttributeNames.Href].Value}";
            return page;
        }

        private bool CheckTag(HtmlNode tag, string baseUrl)
        {
            return (!string.IsNullOrEmpty(tag.Attributes[Consts.AttributeNames.Href]?.Value)
                 && !tag.Attributes[Consts.AttributeNames.Href].Value.StartsWith("#")
                 && !tag.Attributes[Consts.AttributeNames.Href].Value.StartsWith("?")
                 && !tag.Attributes[Consts.AttributeNames.Href].Value.Equals("/")
                 && !tag.Attributes[Consts.AttributeNames.Href].Value.StartsWith("mailto")
                 && !tag.Attributes[Consts.AttributeNames.Href].Value.StartsWith("javascript"));
        }
        private static void GetTagsByName(ref List<HtmlNode> tags, HtmlNodeCollection nodes, string tagName)
        {
            foreach (var node in nodes)
            {
                if (node.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase) ) 
                    tags.Add(node);
                GetTagsByName(ref tags, node.ChildNodes, tagName);
            }
        }

        public static string RemoveInvalidCharacters(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();

                string[] invalidCharacters =
                    new string[]
                    {
                        "#", "%", "&", "*", ":", "<", ">", "?", "\\", "/", "{", "}", "~", "+", "-", ",", "(", ")", "|", ".", "€", "@", "[", "]", "°", "^", "´", "`", "$", "§", "!", "‘", ";", "“", "=", "\"", "–"
                    };

                Regex cleanUpRegex = GetCharacterRemovalRegex(invalidCharacters);
                string cleanName = cleanUpRegex.Replace(name, " ");

                if (cleanName.StartsWith("_", StringComparison.Ordinal))
                    cleanName = cleanName.Substring(1);

                if (cleanName.Length > 120)
                    cleanName = cleanName.Substring(0, 120);
                return cleanName;
            }

            return string.Empty;
        }

        private static Regex GetCharacterRemovalRegex(string[] invalidCharacters)
        {
            string[] escapedCharacters = new string[invalidCharacters.Length];
            int index = 0;
            foreach (string input in invalidCharacters)
            {
                escapedCharacters[index] = Regex.Escape(input);
                index++;
            }
            return new Regex(string.Join("|", escapedCharacters));
        }


    }
}
