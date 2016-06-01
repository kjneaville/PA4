using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WorkerRole1
{
    public class dashStat : TableEntity
    {
        public dashStat(string state, string r, string c, int crawl, string ten, int qS, int tS, string err)
        {
            this.PartitionKey = "Counter";
            this.RowKey = "Key";

            this.wrState = state;
            this.ramUse = r;
            this.cpuUse = c;
            this.crawled = crawl;
            this.lastTen = ten;
            this.qSize = qS;
            this.tSize = tS;
            this.errors = err;
        }

        public dashStat()
        {
            this.PartitionKey = "Counter";
            this.RowKey = "Key";

            this.wrState = "Idle";
            this.ramUse = "0 MB";
            this.cpuUse = "0%";
            this.crawled = 0;
            this.lastTen = "";
            this.qSize = 0;
            this.tSize = 0;
            this.errors = ""; 
        }

        public string wrState { get; set; } //Worker Role Status
        public string ramUse { get; set; } //RAM in MB
        public string cpuUse { get; set; } //CPU %
        public int crawled { get; set; } //URLs Crawled
        public string lastTen { get; set; } //Last 10 URLs Crawled
        public int qSize { get; set; } //URL Queue Size
        public int tSize { get; set; } //Table Size
        public string errors { get; set; } //List of Errors 

    }
}