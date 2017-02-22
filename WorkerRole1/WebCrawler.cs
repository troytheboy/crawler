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

namespace WorkerRole1
{
    class WebCrawler
    {
        private List<string> disallow;

        public WebCrawler(List<string> rules)
        {
            disallow = rules;
        }

        public List<string> CrawlPage(HttpWebResponse response, string url)
        {
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            //ConfigurationManager.AppSettings["StorageConnectionString"]);

            //CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            //CloudTable table = tableClient.GetTableReference("pages");
            //table.CreateIfNotExists();

            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference("crawledUrls");
            table.CreateIfNotExists();


            List<string> links = new List<string>();

            string data = "";
            string[] s = new string[1];
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
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

                data = readStream.ReadLine().Replace(">", ">|");
                string[] htmlTags = data.Split('|');
                string date = "";
                string title = "";
                foreach (string tag in htmlTags)
                {

                    if (tag.Contains("</title>"))
                    {
                        title = tag.Replace("</title>", "");
                    }
                    if (tag.Contains("lastmod"))
                    {
                        date = tag.Substring(15, 19);
                    }



                    if (tag.Contains("<a "))
                    {
                        string[] attributes = tag.Split(' ');
                        foreach (string attribute in attributes)
                        {
                            if (attribute.Contains("href"))
                            {
                                string link = attribute.Substring(5);
                                int length = link.Length;
                                link = link.Replace("\"", "");
                                link = link.Replace(">", "");
                                if (link.StartsWith("//"))
                                {

                                }
                                else if (link.StartsWith("/"))
                                {
                                    if (url.Contains("cnn.com"))
                                    {
                                        link = "http://www.cnn.com" + link;

                                    }
                                    else if (url.Contains("bleacherreport.com"))
                                    {
                                        link = "http://www.bleacherreport.com" + link;
                                    }
                                }
                                if (link.Contains("cnn.com") || link.Contains("bleacherreport.com"))
                                {
                                    bool allowed = true;
                                    foreach (string dis in disallow)
                                    {
                                        if (link.Contains(dis))
                                        {
                                            allowed = false;
                                        }
                                    }
                                    if (allowed)
                                    {
                                        links.Add(link);
                                    }
                                }
                            }
                        }
                    }
                }
                Page crawledPage = new Page(title, date, url);
                TableOperation insertOperation = TableOperation.Insert(crawledPage);
                try {
                    table.Execute(insertOperation);
                }
                catch 
                {
                    //continue;
                }

                response.Close();
                readStream.Close();
                return links;
            }
            return new List<string>();
            //return data;
        }
    }


}
