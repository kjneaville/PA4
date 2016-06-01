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
        public pageData(string url, string pageTitle, string date, string errorMessage, string statusCode)
        {
            this.PartitionKey = "url";
            this.RowKey = HttpUtility.UrlEncode(url);

            this.url = url;
            this.pageTitle = pageTitle;
            this.date = date;
            this.errorMessage = errorMessage;
            this.statusCode = statusCode;
        }

        public pageData()
        {
            this.PartitionKey = "url";
            this.RowKey = "Blank Insert";

            this.url = "";
            this.pageTitle = "";
            this.date = "";
            this.errorMessage = "";
            this.statusCode = "";
        }

        public string url { get; set; } //URL
        public string pageTitle { get; set; } //Title on Webpage
        public string date { get; set; } //Date of Webpage
        public string errorMessage { get; set; } //Error Message (if any)
        public string statusCode { get; set; } //Error Code ("OK" if no error)
    }
}