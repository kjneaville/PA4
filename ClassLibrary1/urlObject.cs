using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace ClassLibrary1
{
    public class urlObject : TableEntity
    {
        public string url { get; set; }

        public urlObject() { }

        public urlObject(string u)
        {
            this.PartitionKey = u;
            this.RowKey = Guid.NewGuid().ToString();
            this.url = u;
        }
    }
}

