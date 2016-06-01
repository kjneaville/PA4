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
        public static CloudTable tableRef = tableClient.GetTableReference("pageData");
        public static Dictionary<string, List<pageData>> cachedResults = new Dictionary<string, List<pageData>>();
        public static Dictionary<string, Tuple<List<pageData>, DateTime>> cachedR = new Dictionary<string, Tuple<List<pageData>, DateTime>>();

        [WebMethod]
        //[ScriptMethod(UseHttpGet = true)]
        public string startCrawling() //Add a message to the command queue to begin Crawling URLs
        {
            queueRef.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("start");
            queueRef.AddMessage(message);
            
            return "ticket added";
        }

        [WebMethod]
        //[ScriptMethod(UseHttpGet = true)]
        public string stopCrawling() //Add a message to the command queue to stop Crawling URLs
        {
            queueRef.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("stop");
            queueRef.AddMessage(message);
            return "crawler";
        }
        [WebMethod]
        //[ScriptMethod(UseHttpGet = true)]
        public string clearIndex() //Add a message to the command queue to clear both queues
        {
            queueRef.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("clear");
            queueRef.AddMessage(message);
            return "queues";
        }
        [WebMethod]
        public string clearTable() //Add a message to the command queue to clear the table
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
        [WebMethod]
        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        public string findUrl(string url) //Called to determine whether or not a URL exists in our table storage
        {
            tableRef.CreateIfNotExists();

            string response = "";
            TableQuery<pageData> query = new TableQuery<pageData>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, HttpUtility.UrlEncode(url)));
            var queryResults = tableRef.ExecuteQuery(query);
            if (queryResults == null)
            {
                response = "Not Found";
            }
            foreach (pageData entity in tableRef.ExecuteQuery(query))
            {
                response = entity.pageTitle;
                break;
            }
            return new JavaScriptSerializer().Serialize(response);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTable(string search) //Used to search our crawled URLs to display the top 20 that are relevant
        {
            tableRef.CreateIfNotExists();

            if (cachedResults.ContainsKey(search))
            {
                return new JavaScriptSerializer().Serialize(cachedResults[search]);
            }
            else
            {
                var words = search.Split(' '); //Search based on matches of each word, not entire string
                TableQuery<pageData> query = new TableQuery<pageData>()
                       .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, words[0].ToLower()));
                var queryResults = tableRef.ExecuteQuery(query);
                for (var i = 1; i < words.Length; i++)
                {
                    TableQuery<pageData> query2 = new TableQuery<pageData>()
                       .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, words[i].ToLower()));
                    queryResults = queryResults.Concat(tableRef.ExecuteQuery(query2)); //Combine all results for all individual words
                }
                var orderedResults = queryResults.GroupBy(x => x.RowKey) //LINQ
                    .OrderByDescending(x => x.ToList().Count)
                    .ThenByDescending(x => x.ToList()[0].date)
                    .Select(x => x.First()).Take(20).ToList();
                List<string> keys = cachedResults.Keys.ToList();
                if (keys.Count > 100)
                {
                    cachedResults.Clear();
                }
                cachedResults.Add(search, orderedResults);
                return new JavaScriptSerializer().Serialize(orderedResults);
            }
        }
    }
}
