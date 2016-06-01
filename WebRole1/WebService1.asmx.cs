using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using WorkerRole1;

namespace WebRole1
{
    /// <summary>
    /// This ASMX file will be called by dashboard.html to insert messages into the command queue. These messages will be dequeued by the worker role.
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        public static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("StorageConnectionString"));
        public static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        public static CloudQueue queueRef = queueClient.GetQueueReference("myqueue");
        public static CloudQueue queueRef2 = queueClient.GetQueueReference("tickets");
        public static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        public static CloudTable dashInfo = tableClient.GetTableReference("dashboard");

        [WebMethod]
        //[ScriptMethod(UseHttpGet = true)]
        public string startCrawling()
        {
            queueRef.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("start");
            queueRef.AddMessage(message);
            
            return "ticket added";
        }

        [WebMethod]
        //[ScriptMethod(UseHttpGet = true)]
        public string stopCrawling()
        {
            queueRef.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("stop");
            queueRef.AddMessage(message);
            return "crawler";
        }
        [WebMethod]
        //[ScriptMethod(UseHttpGet = true)]
        public string clearIndex()
        {
            queueRef.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("clear");
            queueRef.AddMessage(message);
            return "queues";
        }
        [WebMethod]
        public string clearTable()
        {
            queueRef.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("table");
            queueRef.AddMessage(message);
            return "table";
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //Here we retrieve the dashStat entity from our dashboard table (inserted by the Worker role to include relevant dashboard information) to display on our HTML page
        public string getPageTitle() 
        {
            dashInfo.CreateIfNotExists();
            TableOperation retrieveOperation = TableOperation.Retrieve<dashStat>("Counter", "Key");
            TableResult retrievedResult = dashInfo.Execute(retrieveOperation);
            dashStat updateEntity = (dashStat)retrievedResult.Result;
            List<string> result = new List<string>();
            result.Add(updateEntity.wrState);
            result.Add(updateEntity.ramUse);
            result.Add(updateEntity.cpuUse);
            result.Add(updateEntity.crawled.ToString());
            result.Add(updateEntity.lastTen);
            result.Add(updateEntity.qSize.ToString());
            result.Add(updateEntity.tSize.ToString());
            result.Add(updateEntity.errors);
            return new JavaScriptSerializer().Serialize(result);
        }
    }
}
