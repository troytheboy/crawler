using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WorkerRole1
{
    class SiteMapper
    {
        private static readonly string FILE_LOCATION = System.IO.Path.GetTempFileName();
        private List<string> disallow = new List<string>();
        private List<string> sitemaps = new List<string>();
        private List<string> urls = new List<string>();
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue toCrawl = queueClient.GetQueueReference("urls");

        //private static CloudQueue queue;
        private static string site;

        public SiteMapper(string newSite)
        {
            site = newSite;
            getSitemaps();
            getMapUrls();
            Crawl();
        }

        public List<string> getUrls()
        {
            return urls;
        }

        public List<string> getDisallow()
        {
            return disallow;
        }

        private void sitemapsHelper()
        {

        }

        private void getSitemaps()
        {
            // get robots.txt
            WebClient client = new WebClient();
            string rbts = site + "/robots.txt";
            Stream robots = client.OpenRead(site + "/robots.txt");
            StreamReader sr = new StreamReader(robots);

            // get all site maps
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line.StartsWith("Sitemap: "))
                {
                    sitemaps.Add(line.Remove(0, 9));
                }
                if (line.StartsWith("Disallow: "))
                {
                    disallow.Add(site + line.Remove(0, 10));
                }
            }
            // get urls from sitemaps
            List<string> urls = new List<string>();
            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            List<string> indexedMaps = new List<string>();
            if (site.Contains("cnn"))
            {
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
            }
            // add each map from the map index into the sitemaps list
            foreach (string map in indexedMaps)
            {
                sitemaps.Add(map);
            } 
        }

        private void getMapUrls()
        {
            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object

            foreach (string sitemap in sitemaps)
            {
                xmlDoc = new XmlDocument(); // Create an XML document object
                xmlDoc.Load(sitemap);
                XmlNodeList nodes = xmlDoc.GetElementsByTagName("url");
                foreach (XmlNode node in nodes)
                {
                    XmlNodeList children = node.ChildNodes;

                    XmlNode loc = node.FirstChild;
                    urls.Add(loc.InnerText);
                }
            }
        }

        public List<string> Crawl()
        {
            // get robots.txt
            WebClient client = new WebClient();
            try
            {
                Stream cnn = client.OpenRead("http://www.cnn.com/robots.txt");
                Stream bleacher = client.OpenRead("http://www.bleacherreport.com/robots.txt");
                StreamReader sr = new StreamReader(cnn);

                // get all site maps
                List<string> sitemaps = new List<string>();
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line.StartsWith("Sitemap: "))
                    {
                        sitemaps.Add(line.Remove(0, 9));
                    }
                    if (line.StartsWith("Disallow: "))
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
                    if (line.StartsWith("Sitemap: ") && line.Contains("nba"))
                    {
                        sitemaps.Add(line.Remove(0, 9));
                    }
                }

                // get urls from sitemaps
                //List<string> urls = new List<string>();
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

                WebCrawler crawlie = new WebCrawler(disallow);
                WebCrawler webbie = new WebCrawler(disallow);
                //WebCrawler diddler = new WebCrawler(disallow);
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
                        XmlNode loc = node.FirstChild;
                        urls.Add(loc.InnerText);
                        CloudQueueMessage urlToCrawl = new CloudQueueMessage(loc.InnerText);
                        toCrawl.AddMessage(urlToCrawl);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return urls;
        }
    }
}
