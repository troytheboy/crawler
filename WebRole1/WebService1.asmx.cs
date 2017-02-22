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
        private static List<string> disallow = new List<string>();
        private static CloudQueue queue;

        [WebMethod]
        public string startCrawl()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("status");
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("Start");
            queue.AddMessage(message);
            CloudQueueMessage idle = queue.GetMessage();
            return queue.PeekMessage().AsString;
        }

        [WebMethod]
        public string clearQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("myurls");
            queue.CreateIfNotExists();
            queue.Clear();
            return "Queue Cleared";
        }

        [WebMethod]
        public string getStatus()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("status");
            queue.CreateIfNotExists();
            CloudQueueMessage message = queue.PeekMessage();
            return message.AsString;
        }

        [WebMethod]
        public string getLastUrl()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue urls = queueClient.GetQueueReference("urls");
            queue.CreateIfNotExists();

            return urls.PeekMessage().AsString;
        }

        [WebMethod]
        public List<string> Crawl()
        {
            // get robots.txt
            WebClient client = new WebClient();
            Stream cnn = client.OpenRead("http://www.cnn.com/robots.txt");
            Stream bleacher = client.OpenRead("http://www.bleacherreprot.com/robots.txt");
            StreamReader sr = new StreamReader(cnn);

            // get all site maps
            List<string> sitemaps = new List<string>();
            //List<string> disallow = new List<string>();
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if(line.StartsWith("Sitemap: "))
                {
                    sitemaps.Add(line.Remove(0, 9));
                }
                if(line.StartsWith("Disallow: "))
                {
                    disallow.Add("http://cnn.com" + line.Remove(0, 10));
                }
            }
            sr = new StreamReader(bleacher);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line.StartsWith("Disallow: "))
                {
                    disallow.Add("http://bleacherreport.com" + line.Remove(0, 10));
                }
            }

            // get urls from sitemaps
            List<string> urls = new List<string>();
            List<Page> pages = new List<Page>();
            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            List<string> indexedMaps = new List<string>();

            // look through sitemaps for sitemap indexes and add sitemaps from those indexes to a list
            foreach (string sitemap in sitemaps)
            { 
                xmlDoc.Load(sitemap); // Load the XML document from the specified file
                XmlNodeList maps = xmlDoc.GetElementsByTagName("sitemap");
                if (maps.Count > 0)
                {
                    // add to sitemaps list
                    foreach (XmlNode map in maps)
                    {
                        XmlNode loc = map.FirstChild;
                        if (loc.InnerText.Contains("2017"))
                        {
                            //XmlNodeList children = map.ChildNodes;
                            indexedMaps.Add(loc.InnerText);
                        }
                    }
                }
            }

            // add maps from map indexes to sitemaps list
            foreach (string map in indexedMaps)
            {
                sitemaps.Add(map);
            }          
            foreach (string sitemap in sitemaps)
            {
                xmlDoc = new XmlDocument(); // Create an XML document object
                xmlDoc.Load(sitemap);
                XmlNodeList nodes = xmlDoc.GetElementsByTagName("url");
                foreach (XmlNode node in nodes)
                {
                    XmlNodeList children = node.ChildNodes;
                    Page page = new Page();

                    XmlNode loc = node.FirstChild;
                    urls.Add(loc.InnerText);

                    foreach (XmlNode child in children)
                    {
                        string name = child.Name;
                        if (name.Equals("loc"))
                        {
                            page.setUrl(child.InnerText);                     
                        }
                        if (name.Equals("news:news"))
                        {
                            XmlNodeList grandChildren = child.ChildNodes;
                            foreach (XmlNode grandChild in grandChildren)
                            {
                                if (grandChild.Name.Equals("news:publication_date"))
                                {
                                    page.setDate(grandChild.InnerText);
                                }
                                if (grandChild.Name.Equals("news:title"))
                                {
                                    page.setTitle(grandChild.InnerText);
                                }
                            }
                        }                
                    }
                    pages.Add(page);
                }

            }
            List<string> pageString = new List<string>();
            foreach (Page p in pages)
            {
                pageString.Add(p.ToString());
            }
            return pageString;
        }

        public interface Document
        {
            void setTitle(string title);
            string getTitle();
            void setUrl(string url);
            string getUrl();
        }

        public class Page : Document
        {
            private string title;
            private string url;
            private string date;

            public void setTitle(string title)
            {
                this.title = title;
            }

            public string getTitle()
            {
                return this.title;
            }

            public void setUrl(string url)
            {
                this.url = url;
            }

            public string getUrl()
            {
                return this.url;
            }

            public void setDate(string date)
            {
                this.date = date;
            }

            public string getDate()
            {
                return this.date;
            }

            public override string ToString()
            {
                string s = "" + this.getUrl();
                return s;
            }
        }


        [WebMethod]
        public List<string> HtmlTest(string url)
        {
            List<string> links = new List<string>();
            //string url = page
            //string url = "http://www.cnn.com/2017/02/14/politics/donald-trump-aides-russians-campaign/index.html";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string data = "";
            string[] s = new string[1];
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
                
                
                data = readStream.ReadLine().Replace(">",">|");
                string[] htmlTags = data.Split('|');
                foreach (string tag in htmlTags)
                {
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
                                if (link.StartsWith("/"))
                                {
                                    link = "http://www.cnn.com" + link;
                                }
                                if (link.Contains("http://www.cnn.com"))
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
                response.Close();
                readStream.Close();
                return links;
            }
            return s.ToList();
            //return data;
        }
    }
}
