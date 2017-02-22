using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class Page : TableEntity
    {
        private string url;

        public Page(string title, string date, string url)
        {
            this.PartitionKey = date;
            this.RowKey = title;
            this.url = url;
        }

        public Page() { }

        public string getUrl { get; set; }
    }
}
