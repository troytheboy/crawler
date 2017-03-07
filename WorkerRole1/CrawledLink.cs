//using Microsoft.WindowsAzure.Storage.Table;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace WorkerRole1
//{
//    public class CrawledLink : TableEntity
//    {
//        public string url;
//        private string date;
//        private string title;

//        public CrawledLink(string title, DateTime date, string url)
//        {
//            this.PartitionKey = title.ToLower();
//            this.RowKey = url.Replace('/', '$');
//            this.url = url;
//            this.date = date.ToString();
//        }

//        public CrawledLink() { }

//        public string getUrl() { return this.url; }
//        public string toString() { return "URL: " + this.url + "\t PK: " + this.PartitionKey + "\t RK: " + this.RowKey; }
//    }
//}
