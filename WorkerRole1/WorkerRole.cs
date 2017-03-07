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
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue statusQueue = queueClient.GetQueueReference("status");
        private static CloudQueue toCrawl = queueClient.GetQueueReference("urls");
        private static CloudQueue crawled = queueClient.GetQueueReference("crawled");


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
            try
            {
                statusQueue.CreateIfNotExists();
                statusQueue.Clear();
                CloudQueueMessage loading = new CloudQueueMessage("Loading...");
                statusQueue.AddMessage(loading);

                toCrawl.CreateIfNotExists();
                toCrawl.Clear();

                crawled.CreateIfNotExists();
                crawled.Clear();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.Expect100Continue = false;

            bool result = base.OnStart();
            try
            {
                updateStatus("Crawling");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            // create sitemappers
            SiteMapper cnnMapper = new SiteMapper("http://www.cnn.com");

            List<string> cnnUrls = cnnMapper.getUrls();
            List<string> disallow = cnnMapper.getDisallow();

            Trace.TraceInformation("WorkerRole1 has been started");

            updateStatus("Idle");

            //while (true)
            //{
            //    string statusMessage = peekStatus();
            //    if (statusMessage.Equals("Start"))
            //    {
            //        //start crawling
            //        updateStatus("Crawling");
            //        //crawl(crawlie);
            //    }
            //    Thread.Sleep(1000);
            //}

            return result;
        }

        //private void crawl(WebCrawler crawlie)
        //{
        //    CloudQueueMessage message = toCrawl.PeekMessage();
        //    while(message != null)
        //    {
        //        message = toCrawl.GetMessage();
        //        toCrawl.DeleteMessage(message);
        //        string url = message.AsString;
        //        string cnn = "cnn.com";
        //        string br = "bleacherreport.com";
        //        if (url.Contains('.'))
        //        {
        //            if (url.StartsWith("//"))
        //            {
        //                url = url.Replace("//", "http://www.");
        //            }
        //            try
        //            {
        //                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //                request.Proxy = null;
        //                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //                if (url.Contains(cnn))
        //                {
        //                    crawlie.CrawlPage(response, url, cnn);
        //                }
        //                else if (url.Contains(br))
        //                {
        //                    crawlie.CrawlPage(response, url, br);
        //                }
        //            }
        //            catch (WebException we)
        //            {
        //                if (we.Equals("test"))
        //                {

        //                }
        //                continue;
        //            }
        //        }
        //    }
        //}

        private void updateStatus(string statusMessage)
        {
            CloudQueueMessage message = statusQueue.GetMessage();
            statusQueue.DeleteMessage(message);
            CloudQueueMessage update = new CloudQueueMessage(statusMessage);
            statusQueue.AddMessage(update);
        }

        private string peekStatus()
        {
            CloudQueueMessage message = statusQueue.PeekMessage();
            if (message == null)
            {
                return "Switching Status";
            } else
            {
                return message.AsString;

            }
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
