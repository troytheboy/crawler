using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class CrawledLink : TableEntity
    {
        public string url;
        public string title;
        //private string title;

        public CrawledLink(string word, string urlTitle, string url)
        {
            this.PartitionKey = word.ToLower();
            this.RowKey = url.Replace('/', '$');
            this.url = url;
            title = urlTitle;
            //this.date = date.ToString();
        }

        public CrawledLink() { }

        public string getUrl() { return this.url; }
        public string toString() { return "URL: " + this.url + "\t PK: " + this.PartitionKey + "\t RK: " + this.RowKey; }
    }
}
