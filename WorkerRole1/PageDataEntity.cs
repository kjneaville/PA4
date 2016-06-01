using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WorkerRole1
{
    public class pageData : TableEntity
    {
        public pageData(string wordKey, string url, string pageTitle, string pageText, string date, string errorMessage, string statusCode)
        {
            this.PartitionKey = wordKey;
            this.RowKey = HttpUtility.UrlEncode(url);

            this.url = url;
            this.pageTitle = pageTitle;
            this.date = date;
            this.errorMessage = errorMessage;
            this.statusCode = statusCode;
            this.pageText = pageText;
        }

        public pageData()
        {
            this.PartitionKey = "wordKey";
            this.RowKey = "Blank Insert";

            this.url = "";
            this.pageTitle = "";
            this.pageText = "";
            this.date = "";
            this.errorMessage = "";
            this.statusCode = "";
        }

        public string url { get; set; }
        public string pageTitle { get; set; }
        public string pageText { get; set; }
        public string date { get; set; }
        public string errorMessage { get; set; }
        public string statusCode { get; set; }

    }
}
