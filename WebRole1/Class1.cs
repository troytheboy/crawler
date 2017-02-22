using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRole1
{
    public class Page : TableEntity
    {
        private string url;
        private string date;

        public Page(string title, string date, string url)
        {
            this.PartitionKey = date.Substring(0, 4);
            this.RowKey = date.Substring(5, 5);
            this.url = url;
            this.date = date;
        }

        public Page() { }

        public string getUrl { get; set; }
    }
}
