using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Xml;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using System.Web.Script.Services;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using WorkerRole1;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        private static readonly string FILE_LOCATION = System.IO.Path.GetTempFileName();

        PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes"); private static List<string> disallow = new List<string>();

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue statusQueue = queueClient.GetQueueReference("status");
        private static CloudQueue toCrawl = queueClient.GetQueueReference("urls");
        private static CloudQueue lastCrawled = queueClient.GetQueueReference("crawled");
        //private static CloudQueue numCrawledQueue = queueClient.GetQueueReference("num-crawled");



        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void startCrawl()
        {
            CloudQueueMessage crawling = new CloudQueueMessage("Start");
            statusQueue.AddMessageAsync(crawling);
            CloudQueueMessage idle = statusQueue.GetMessage();
            statusQueue.DeleteMessage(idle);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getNumCrawled()
        {
            try
            {
                lastCrawled.FetchAttributes();
                var crawledCount = lastCrawled.ApproximateMessageCount;
                return crawledCount.ToString();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return "0";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> getCrawled(string searchQuery)
        {
            List<string> s = new List<string>();
            string[] splitQuery = searchQuery.Split(' ');
            List<string> urls = new List<string>();
            //Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            try
            {
                CloudTable table = tableClient.GetTableReference("crawled");
                table.CreateIfNotExists();
                foreach (string word in splitQuery)
                {
                    TableQuery<CrawledLink> query = new TableQuery<CrawledLink>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word));
                    foreach (CrawledLink entity in table.ExecuteQuery(query))
                    {
                        string link = entity.RowKey.Replace('$', '/');
                        urls.Add(link);
                    }
                }
                Dictionary<string, int> ranking = new Dictionary<string, int>();
                foreach(string url in urls)
                {
                    int urlCount = 1;
                    if (ranking.Keys.Contains(url))
                    {
                        ranking.TryGetValue(url, out urlCount);
                        urlCount++;
                    }
                    ranking.Add(url, urlCount);
                }
                List<int> occurances = new List<int>();
                foreach (KeyValuePair<string, int> urlRank in ranking)
                {
                    occurances.Add(urlRank.Value);
                }
                Dictionary<int, List<string>> switcharoo = new Dictionary<int, List<string>>();
                //occurances.Sort((a, b) => -1 * a.CompareTo(b));
                foreach (KeyValuePair<string, int> urlRank in ranking)
                {
                    List<string> urlsList = new List<string>();
                    //occurances.Add(urlRank.Value);
                    if (switcharoo.Keys.Contains(urlRank.Value))
                    {
                        switcharoo.TryGetValue(urlRank.Value, out urlsList);
                        urlsList.Add(urlRank.Key);
                        switcharoo.Add(urlRank.Value, urlsList);
                    } else
                    {
                        urlsList.Add(urlRank.Key);
                        switcharoo.Add(urlRank.Value, urlsList);
                    }
                }
                urls = new List<string>();
                foreach(KeyValuePair<int, List<string>> urlRank in switcharoo)
                {
                    foreach(string url in urlRank.Value)
                    {
                        urls.Add(url);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return urls;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getToCrawl()
        {
            try
            {
                toCrawl.FetchAttributes();
                var messageCount = toCrawl.ApproximateMessageCount;
                return messageCount.ToString();
            }
            catch (Exception)
            {
            }

            return "0";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getStatus()
        {

            CloudQueueMessage message = statusQueue.PeekMessage();
            if (message != null)
            {
                return message.AsString;
            }
            return "Updating...";
        }
        [WebMethod]
        public String downloadWiki()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("wiki");
            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        File.WriteAllText(FILE_LOCATION, blob.DownloadText());
                        return "File saved at " + FILE_LOCATION;
                    }
                }
            }
            return "";
        }
        public static Trie t = new Trie();
        [WebMethod]
        public Trie buildTrie()
        {
            PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(FILE_LOCATION))
                {
                    int n = 300;
                    while (!sr.EndOfStream)
                    {
                        string s = sr.ReadLine();
                        t.AddWord(s.ToLower());
                        if (n % 1000 == 0)
                        {
                            if (memProcess.NextValue() <= 20)
                            {
                                return t;
                            }
                        }
                        n--;
                    }
                }
                return t;
            }
            catch (Exception e)
            {
                Debug.WriteLine("The file could not be read:");
                Debug.WriteLine(e.Message);
            }
            return t;
        }
        public List<string> titles = new List<string>();
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string[] searchTrie(string query)
        {
            string[] results = new string[10];
            var count = 0;
            ICollection<string> matches = t.GetWords(query.ToLower());
            foreach (string match in matches)
            {
                if (count == 10)
                {
                    return results;
                }
                else
                {
                    results[count] = match;
                }
                count++;
            }
            return results;
        }
    }

}
