using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Azure;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Diagnostics;

namespace WorkerRole1
{
    class WebCrawler
    {
        private List<string> disallow;
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue toCrawl = queueClient.GetQueueReference("urls");
        private static CloudQueue status = queueClient.GetQueueReference("status");
        private static CloudQueue crawled = queueClient.GetQueueReference("crawled");
        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable table = tableClient.GetTableReference("crawled");
        private static List<string> links = new List<string>();
        private string title;

        public WebCrawler(List<string> rules)
        {
            disallow = rules;
            crawl();
        }

        private async void crawl()
        {
            while (true) {
                try
                {
                    CloudQueueMessage message = await toCrawl.PeekMessageAsync();
                    CloudQueueMessage statusMessage = await status.PeekMessageAsync();
                    while (message != null && statusMessage.AsString == "Start")
                    {
                        message = await toCrawl.GetMessageAsync();
                        try
                        {
                            await toCrawl.DeleteMessageAsync(message);
                            string url = message.AsString;
                            string cnn = "cnn.com";
                            string br = "bleacherreport.com";
                            if (url.Contains('.'))
                            {
                                if (url.StartsWith("//"))
                                {
                                    url = url.Replace("//", "http://www.");
                                }
                                try
                                {
                                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                                    request.Proxy = null;
                                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                                    if (url.Contains(cnn))
                                    {
                                        await CrawlPage(response, url, cnn);
                                    }
                                    else if (url.Contains(br))
                                    {
                                        await CrawlPage(response, url, br);
                                    }
                                }
                                catch (Exception we)
                                {
                                    Debug.WriteLine(we);
                                }
                            }
                            await Task.Delay(1000);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                await Task.Delay(1000);
            }
        }

        public async Task CrawlPage(HttpWebResponse response,string address, string url)
        {
            string link = "";
            Boolean illegalLink = false;
            // check disallowed from robots
            foreach (string disallowed in disallow)
            {
                if(url.Contains(disallowed))
                {
                    illegalLink = true;
                }
            }
            if (response.StatusCode == HttpStatusCode.OK && !illegalLink)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }
                string wholePage = readStream.ReadToEnd();
                var titleRegex = new Regex("<title>(.*?[^\"])</title>");
                var titleMatch = titleRegex.Match(wholePage);
                this.title = titleMatch.ToString();
                this.title = this.title.Replace("<title>", "");
                this.title = this.title.Replace("</title>", "");
                string[] titleArray = this.title.Trim().Replace(" - ", " ").Split(' ');
                var regex = new Regex("<a\\shref=[\"]([^\"]*)");
                var matches = regex.Matches(wholePage);
                foreach (Match match in matches)
                {
                    link = match.ToString();
                    link = link.Substring(9);
                    if (link.StartsWith("//"))
                    {
                        link = link.Replace("//", "http://www.");
                    } else if (link.StartsWith("/"))
                    {
                        if (url.Contains("cnn.com"))
                        {
                            link = "http://www.cnn.com" + link;
                        }
                        if (url.Contains("bleacherreport.com"))
                        {
                            link = "http://www.bleacherreport.com" + link;
                        }
                    }
                    if (link.Contains("cnn.com") || link.Contains("bleacherreport.com"))
                    {
                        links.Add(link);
                        CloudQueueMessage urlToCrawl = new CloudQueueMessage(link);
                        try
                        {
                            await toCrawl.AddMessageAsync(urlToCrawl);

                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                }
                if (address.Contains("cnn.com") || address.Contains("bleacherreport.com"))
                {
                    DateTime crawlTime =  DateTime.Today;
                    char[] noNos = { ':',';',',','?','$','(',')','+','=','-','"' };
                    string firstWord = titleArray[0];
                    foreach (char c in noNos)
                    {
                        firstWord.Replace(c + "", "");
                    }
                    if (firstWord.StartsWith("'"))
                    {
                        firstWord = firstWord.Substring(1);
                    }
                    string linkAddress = address.Replace('/', '$');
                    firstWord = firstWord.ToLower();
                    TableOperation retrieveOperation = TableOperation.Retrieve<CrawledLink>(firstWord, linkAddress);
                    try
                    {
                        TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
                        if (retrievedResult.Result == null)
                        {
                            foreach (string word in titleArray)
                            {
                                string wordCopy = word.ToLower();
                                if (wordCopy != "")
                                {
                                    foreach (char c in noNos)
                                    {
                                        wordCopy = word.Replace(c + "", "");
                                    }
                                    if (wordCopy.StartsWith("'"))
                                    {
                                        wordCopy = wordCopy.Substring(1);
                                    }
                                    CrawledLink crawledPage = new CrawledLink(wordCopy, title, address);
                                    TableOperation insertOperation = TableOperation.InsertOrMerge(crawledPage);
                                    try
                                    {
                                        await table.ExecuteAsync(insertOperation);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine(address + "\n" + "Error: " + e.ToString());
                                    }
                                }
                            }
                            CloudQueueMessage crawledURL = new CloudQueueMessage(address);
                            try
                            {
                                await crawled.AddMessageAsync(crawledURL);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Error in WebCrawler-CrawlPage: "+e.ToString());
                    }
                }
                response.Close();
                readStream.Close();
            }
        }
    }
}
