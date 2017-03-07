using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace WebRole1
{

    public class Storage
    {
        //private static CloudQueue status;

        static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);

        static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

        static CloudQueue statusQueue = queueClient.GetQueueReference("status");




    }
}