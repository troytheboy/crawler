using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using WebRole1;
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static CloudQueue status;
        private List<string> crawled = new List<string>();
        private static CloudQueue urls;

        public override void Run()
        {

            

            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            status = queueClient.GetQueueReference("status");
            status.CreateIfNotExists();

            CloudQueueMessage message = new CloudQueueMessage("Loading");
            status.Clear();
            status.AddMessage(message);

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            // create sitemappers
            SiteMapper cnnMapper = new SiteMapper("http://www.cnn.com");
            List<string> cnnUrls = cnnMapper.getUrls();
            List<string> disallow = cnnMapper.getDisallow();
            SiteMapper brMapper = new SiteMapper("http://www.bleacherreport.com");
            //List<string> brUrls = brMapper.getUrls();
            disallow.AddRange(brMapper.getDisallow());

            
            urls = queueClient.GetQueueReference("urls");
            urls.CreateIfNotExists();

            // create web crawlers
            WebCrawler crawlie = new WebCrawler(disallow);
            int count = 0;
            foreach (string url in cnnUrls)
            {
                count++;
                if (count > 10)
                {
                    break;
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    List<string> pageUrls = crawlie.CrawlPage(response, url);
                    foreach (string crawledUrl in pageUrls)
                    {
                        if (!crawled.Contains(crawledUrl))
                        {
                            CloudQueueMessage QueueMessage = new CloudQueueMessage(crawledUrl);
                            urls.AddMessage(QueueMessage);
                            crawled.Add(crawledUrl);
                        }
                    }
                }
                catch (WebException we)
                {
                    if (we.Equals("test"))
                    {

                    }
                    continue;
                }
            }
            Trace.TraceInformation("WorkerRole1 has been started");

            while (true)
            {
                CloudQueueMessage statusMessage = status.PeekMessage();
                if (statusMessage.AsString.Equals("Start"))
                {
                    //start crawling
                    statusMessage = new CloudQueueMessage("Crawling");
                    status.AddMessage(statusMessage);
                    Debug.WriteLine("Message from queue: " + message.AsString);
                    status.AddMessage(statusMessage);
                    status.GetMessage();
                }
                Thread.Sleep(1000);
            }

            //return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
