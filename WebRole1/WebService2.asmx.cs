using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService2 : System.Web.Services.WebService
    {
        private static Trie myTrie;
        private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
        public static string lastWord = "";
        public static int titleCount = 0;

        [WebMethod]
        public string downloadWiki()  //Download our Wiki file from Blob storage
        {
            var filePath = System.IO.Path.GetTempPath() + "\\localWikiFinal.txt";
            Console.Write(filePath);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString2"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("wiki");

            // Retrieve reference to a blob named "photo1.jpg".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("localWikiFinal.txt");
            Console.WriteLine(blockBlob);

            // Save blob contents to a file.
            using (var fileStream = System.IO.File.OpenWrite(filePath))
            {
                blockBlob.DownloadToStream(fileStream);
            }
            return "Success! Your file can be found at " + filePath;
        }
        [WebMethod]
        public string buildTree() //Build the Trie using our downloaded Wiki file (must call downloadWiki() first!)
        {
            myTrie = new Trie();
            var filePath = System.IO.Path.GetTempPath() + "\\localWikiFinal.txt";
            Console.Write(filePath);
            bool mem = true; //Keeps track if we still have memory left
            StreamReader sr = new StreamReader(filePath);
            titleCount = 0;
            while (sr.EndOfStream == false) //While there are still lines to be read and the memory limit has not been reached
            {
                string temp = sr.ReadLine();
                myTrie.addWord(temp);
                lastWord = temp;
                titleCount++;

                if (titleCount % 1000 == 0) //Only check every 1000 iterations because of text file is massive
                {
                    float memory = theMemCounter.NextValue();
                    if (memory < 50)
                    {
                        break;
                    }
                }
            }

            sr.Close();

            return "Success! Navigate to index.html to test it out "; ;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //Get the number of titles added into the trie
        public int getTitleCount()
        {
            return titleCount;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //Get the last title added to the trie
        public string getLastTitle()
        {
            return lastWord;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //Search the Trie to return query suggestions
        public string searchQ(string search)
        {
            List<string> result = myTrie.searchTree(search);
            return new JavaScriptSerializer().Serialize(result);
        }
    }
}
