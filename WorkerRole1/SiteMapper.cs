using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
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
        //private static CloudQueue queue;
        private static string site;

        public SiteMapper(string newSite)
        {
            site = newSite;
            getSitemaps();
            getMapUrls();
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
            Stream robots = client.OpenRead(site + "/robots.txt");
            //Stream cnn = client.OpenRead("http://www.cnn.com/robots.txt");
            //Stream bleacher = client.OpenRead("http://www.bleacherreprot.com/robots.txt");
            StreamReader sr = new StreamReader(robots);

            // get all site maps
            //List<string> sitemaps = new List<string>();
            //List<string> disallow = new List<string>();
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
            //List<Page> pages = new List<Page>();
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
            Stream cnn = client.OpenRead("http://www.cnn.com/robots.txt");
            Stream bleacher = client.OpenRead("http://www.bleacherreprot.com/robots.txt");
            StreamReader sr = new StreamReader(cnn);

            // get all site maps
            List<string> sitemaps = new List<string>();
            //List<string> disallow = new List<string>();
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
            }

            // get urls from sitemaps
            List<string> urls = new List<string>();
            //List<Page> pages = new List<Page>();
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

                    XmlNode loc = node.FirstChild;
                    urls.Add(loc.InnerText);

                    //foreach (XmlNode child in children)
                    //{
                    //    string name = child.Name;
                    //    if (name.Equals("loc"))
                    //    {
                    //        page.setUrl(child.InnerText);
                    //    }
                    //    if (name.Equals("news:news"))
                    //    {
                    //        XmlNodeList grandChildren = child.ChildNodes;
                    //        foreach (XmlNode grandChild in grandChildren)
                    //        {
                    //            if (grandChild.Name.Equals("news:publication_date"))
                    //            {
                    //                page.setDate(grandChild.InnerText);
                    //            }
                    //            if (grandChild.Name.Equals("news:title"))
                    //            {
                    //                page.setTitle(grandChild.InnerText);
                    //            }
                    //        }
                    //    }
                    //}
                    //pages.Add(page);
                }
            }
            return urls;
        }
    }
}
