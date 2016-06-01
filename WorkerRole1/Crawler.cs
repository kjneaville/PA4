using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Web;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WorkerRole1
{
    public class Crawler
    {
        public static List<string> baseUrls = new List<string>();
        public static Queue<string> sitemap = new Queue<string>();  //Sitemap of our baseURL
        public static List<string> disallowedUrls;                  //Respect robots.txt

        public static CloudStorageAccount storageAccount;           //To connect to Azure storage
        public static CloudQueueClient queueClient;
        public static CloudQueue queueRef;
        public static CloudQueue queueRef2;
        public static CloudTableClient tableClient;
        public static CloudTable tableRef;
        public static CloudTable dashInfo;

        public static int crawlerStatus = 0;                         //-1 if nothing to crawl, 0 if stopped, 1 if crawling
        public int countCrawl = 0;                                   //Counter to track of URLs crawled
        public int countTable;                                       //Counter to track table size
        public Queue<string> lTen = new Queue<string>(10);           //List to store last 10 URLs
        public string errList = "";                                  //List of errors concatenated into string (seperated with '$')

        public PerformanceCounter RAM = new PerformanceCounter("Memory", "Available MBytes");
        public PerformanceCounter CPU = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        public string name; //Name of crawler, even though I only have one it needed identity and would make it easier to add multiple

        public Crawler(string x)
        {
            this.name = x;
        }


        public void OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            if (baseUrls.Count == 0)
            {
                baseUrls.Add("http://cnn.com");
                baseUrls.Add("http://bleacherreport.com");
            }
            countTable = 0;
            disallowedUrls = new List<string>();
            storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("StorageConnectionString"));
            queueClient = storageAccount.CreateCloudQueueClient();
            queueRef = queueClient.GetQueueReference("myqueue");
            queueRef.CreateIfNotExists();
            queueRef2 = queueClient.GetQueueReference("tickets");
            queueRef2.CreateIfNotExists();
            tableClient = storageAccount.CreateCloudTableClient();
            dashStat curr = new dashStat(getCrawlerStatus(), "0 MB", "0%", 0, "", 0, 0, errList); //Default dashboard stats, need something to replace
            dashInfo = tableClient.GetTableReference("dashboard");
            tableRef = tableClient.GetTableReference("pageData");
            dashInfo.CreateIfNotExists();
            tableRef.CreateIfNotExists();
            TableOperation insert2 = TableOperation.InsertOrReplace(curr);
            dashInfo.Execute(insert2);
            if (crawlerStatus.Equals("1")) //Only if crawling
            {
                startSitemap();
            }
        }

        public void Run()
        {
            while (true) //Always
            {
                try
                {
                    Thread.Sleep(500);
                    CloudQueueMessage com = queueRef.GetMessage(TimeSpan.FromMinutes(5)); //Check command queue
                    if (com != null)
                    {
                        queueRef.DeleteMessage(com);
                        string comPhrase = com.AsString;
                        if (comPhrase.Equals("stop")) //stop crawl
                        {
                            stopCrawl();
                        }
                        else if (comPhrase.Equals("start")) //start crawl
                        {
                            startCrawl();
                        }
                        else if (comPhrase.Equals("clear")) //clear index
                        {
                            clearIndex();
                        }
                        else if (comPhrase.Equals("table")) //clear table
                        {
                            clearTable();
                        }
                    }
                    if (crawlerStatus != 0) //If not stopped
                    {
                        CloudQueueMessage urlToProcess = queueRef2.GetMessage(TimeSpan.FromMinutes(5)); //Check URL queue
                        if (urlToProcess != null)
                        {
                            ProcessHTML(urlToProcess.AsString);
                            queueRef2.DeleteMessage(urlToProcess);
                        }
                    }
                }
                catch(Exception e)
                {
                    errList += "$" + e;
                }
            }

        }

        //Parse the robots.txt file to initialize sitemap queue
        public void startSitemap()
        {
            //Only start when crawlerStatus is -1
            if (crawlerStatus == -1)
            {
                foreach (string baseUrl in baseUrls)
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(baseUrl + "/robots.txt");
                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                    Stream stream = res.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    List<string> lines = new List<string>();
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        string[] words = line.Split(new char[] { ' ' });
                        if (line.Contains("Sitemap"))
                        {
                            if (baseUrl.Contains("bleacherreport.com"))
                            {
                                if (line.Contains("/nba"))
                                {
                                    sitemap.Enqueue(words[1]);
                                }
                            }
                            else
                            {
                                sitemap.Enqueue(words[1]);
                            }
                        }
                        else if (line.Contains("Disallow"))
                        {
                            disallowedUrls.Add(words[1]); //Not allowed, make sure not crawled later
                        }
                        line = reader.ReadLine();
                    }
                }
                startXML();
            }
        }

        public void startXML()
        {
            while (sitemap.Count != 0)
            {
                string curUrl = sitemap.Dequeue();
                if (curUrl.Length > 0 || curUrl != "")
                {
                    ProcessXML(curUrl);
                }
            }
        }

        //Process an XML Page of links and add the links appropriately to the data storage
        public void ProcessXML(string url)
        {
            if (url.Length > 0 || url != null)
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(url);
                    XmlNodeList siteNodes = xmlDoc.GetElementsByTagName("sitemap");    
                    XmlNodeList urlNodes = xmlDoc.GetElementsByTagName("url");
                    if (siteNodes.Count > 0)
                    {
                        foreach (XmlNode node in siteNodes)
                        {
                            XmlNode xLocNode = node["loc"];
                            XmlNode xDateNode = node["lastmod"];
                            string curUrl = xLocNode.InnerText;
                            if (UrlAllowed(curUrl)) //Url is not in dissallowed list
                            {
                                if (xDateNode == null)
                                {
                                    sitemap.Enqueue(curUrl);
                                }
                                else {
                                    string curDate = xDateNode.InnerText;
                                    DateTime currentDate = (DateTime)Convert.ToDateTime(curDate);
                                    DateTime compareDate = (DateTime)Convert.ToDateTime("04/01/2016");
                                    if (currentDate >= compareDate)
                                    {
                                        sitemap.Enqueue(curUrl);
                                    }
                                }
                            }
                        }
                    }
                    if (urlNodes.Count > 0)
                    {
                        foreach (XmlNode node in urlNodes)
                        {
                            XmlNode locNode = node["loc"];
                            XmlNode dateNode = node["lastmod"];
                            string curUrl = locNode.InnerText;
                            if (UrlAllowed(curUrl))
                            {
                                //No lastdatemod value to check or after April 1st, 2016
                                if (dateNode == null)
                                {
                                    CloudQueueMessage x = new CloudQueueMessage(curUrl);
                                    queueRef2.AddMessage(x);
                                }
                                else
                                {
                                    string curDate = dateNode.InnerText;
                                    DateTime curDateObj = (DateTime)Convert.ToDateTime(curDate);
                                    DateTime compareDate = (DateTime)Convert.ToDateTime("04/01/2016");
                                    if (curDateObj >= compareDate)
                                    {
                                        CloudQueueMessage x = new CloudQueueMessage(curUrl);
                                        queueRef2.AddMessage(x);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.IO.FileNotFoundException e)
                {
                    errList += "$" + e + " at " + url;
                    throw e;
                }
            }
            crawlerStatus = 0;
        }

        public void ProcessHTML(string url)
        {
            string baseUrl = "http://cnn.com";
            foreach (string baseCheck in baseUrls)
            {
                if (url.Contains(baseCheck))
                {
                    baseUrl = baseCheck;
                }
            }
            if (crawlerStatus >= 0)
            {
                if (!tableContains(url))
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    countCrawl++;
                    lTen.Enqueue(url);
                    while (lTen.Count > 10)
                    {
                        lTen.Dequeue();
                    }
                    string pageTitle = "";
                    string pageText = "";
                    string date = "";
                    string statusCode = "";
                    string errorMessage = "";
                    try
                    {
                        //Grab the pages title & date
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        statusCode = response.StatusCode.ToString();

                        Stream responseStream = response.GetResponseStream();
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string pageHtml = reader.ReadToEnd();
                            HtmlDocument htmlDoc = new HtmlDocument();
                            htmlDoc.LoadHtml(pageHtml);

                            //Scrape extra a links on the html page
                            var aTags = htmlDoc.DocumentNode.SelectNodes("//a[@href]");
                            if (aTags != null)
                            {
                                foreach (var tag in aTags)
                                {
                                    if (tag.Attributes["href"].Value != null || tag.Attributes["href"].Value != "")
                                    {
                                        string urlLink = tag.Attributes["href"].Value;
                                        if (urlLink.Equals("/"))
                                        {
                                            urlLink = baseUrl + urlLink;
                                        }

                                        else if (urlLink.Length > 1)
                                        {   //Fix relative path urls that do not start with '//', only '/'
                                            if (urlLink[0].Equals('/') && !urlLink[1].Equals('/'))
                                            {
                                                urlLink = baseUrl + urlLink;
                                            }
                                            //Fix urls that start with '//'
                                            else if (urlLink.StartsWith("//"))
                                            {
                                                urlLink = "http:" + urlLink;
                                            }
                                        }
                                        //Fix urls that end with '/' to add suffix 'index.html'
                                        if (urlLink.Length > 0)
                                        {
                                            if (urlLink[urlLink.Length - 1].Equals('/'))
                                            {
                                                urlLink = urlLink + "index.html";
                                            }
                                        }
                                        //Now have a complete urlLink
                                        if (UrlAllowed(urlLink) && urlLink.Contains(".htm") && (urlLink.Contains("cnn.com") || urlLink.Contains("bleacherreport.com/nba")))
                                        {
                                            CloudQueueMessage newUrl = new CloudQueueMessage(urlLink);
                                            queueRef2.AddMessage(newUrl);
                                        }
                                    }
                                }
                            }

                            var titleTag = htmlDoc.DocumentNode.SelectSingleNode("//title");

                            if (titleTag != null)
                            {
                                pageTitle = titleTag.InnerText;
                            }
                            var pageDate = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='lastmod']");
                            if (pageDate != null)
                            {
                                date = pageDate.Attributes["content"].Value.Substring(0, 10);
                            }
                            var pageNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                            pageText = cleanHTML(pageNode);
                        }
                    }
                    //400 or 500 error
                    catch (WebException we)
                    {
                        errorMessage = we.Message;
                        errList += "$" + errorMessage + " at " + url;
                    }
                    countTable++;
                    updateDash();

                    //Chop off the ' - CNN.com' or ' | Bleacher Report'
                    string pattern;
                    if (baseUrl == "http://cnn.com")
                    {
                        pattern = " - CNN.com";
                    }
                    else
                    {
                        pattern = " | Bleacher Report";
                    }
                    Regex clean = new Regex(pattern);
                    string cleanTitle = clean.Replace(pageTitle, "");

                    char[] delim = { ' ', '-', '!', '(', ')', ',', '.', '?', ':', ';', '{', '}', '&', '$', '/', '\\', '|', '\'', '\"' };
                    List<string> titleWords = cleanTitle.Split(delim).ToList();
                    //Index all of the body word texts as well
                    List<string> bodyWords = pageText.Split(delim).ToList();
                    foreach (string word in bodyWords)
                    {
                        titleWords.Add(word);
                    }
                    foreach (string title in titleWords)
                    {
                        string titleWord = title.ToLower();
                        if (!titleWord.Equals(null) && (titleWord.Length > 2) && !titleWord.Equals("the") && !titleWord.Equals("and") && !titleWord.Equals("but") && !titleWord.Equals("cnn") &&
                            !titleWord.Equals("000") && !titleWord.Equals("nbsp")) ;
                        {
                            pageData scrapedData = new pageData(titleWord, url, pageTitle, pageText, date, errorMessage, statusCode);
                            TableOperation insert = TableOperation.InsertOrReplace(scrapedData);
                            tableRef = tableClient.GetTableReference("pageData");
                            tableRef.Execute(insert);
                        }
                    }           
                }
            }
        }

        //Remove extra children node and junk from an html body node element and return its inner html
        //text as a string up to 1000 characters in length
        private string cleanHTML(HtmlNode pageNode)
        {
            var scriptNodes = pageNode.SelectNodes("//script");
            //Get Rid of all of the script tags
            if (scriptNodes != null)
            {
                foreach (var node in scriptNodes)
                {
                    node.Remove();
                }
            }
            var footerNodes = pageNode.SelectNodes("//footer");
            if (footerNodes != null)
            {
                foreach (var node in footerNodes)
                {
                    node.Remove();
                }
            }
            var iframNodes = pageNode.SelectNodes("//iframe");
            if (iframNodes != null)
            {
                foreach (var node in iframNodes)
                {
                    node.Remove();
                }
            }
            var navNodes = pageNode.SelectNodes("//nav");
            if (navNodes != null)
            {
                foreach (var node in navNodes)
                {
                    node.Remove();
                }
            }
            var spanNodes = pageNode.SelectNodes("//span");
            if (spanNodes != null)
            {
                foreach (var node in spanNodes)
                {
                    node.Remove();
                }
            }
            var updateTimeNodes = pageNode.SelectNodes("//p[@class='update-time']");
            if (updateTimeNodes != null)
            {
                foreach (var node in updateTimeNodes)
                {
                    node.Remove();
                }
            }

            var captionNodes1 = pageNode.SelectNodes("//div[@class='el__gallery-showhide']");
            if (captionNodes1 != null)
            {
                foreach (var node in captionNodes1)
                {
                    node.Remove();
                }
            }

            var captionNodes2 = pageNode.SelectNodes("//div[@class='el__gallery-photocount']");
            if (captionNodes2 != null)
            {
                foreach (var node in captionNodes2)
                {
                    node.Remove();
                }
            }

            var cleanPageNode = pageNode.InnerHtml;
            string pageText = pageNode.InnerText;
            //Get rid of </form> tags
            Regex cleanBody1 = new Regex("</form>");
            pageText = cleanBody1.Replace(pageText, "");
            //Get rid of HTML comments
            pageText = Regex.Replace(pageText, "<!--.*?-->", "", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //Condense multiple spaces to a single space
            Regex spaces = new Regex(@"[ ]{2,}");
            pageText = spaces.Replace(pageText, @" ");
            pageText = Regex.Replace(pageText, "Hide Caption", "", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //Only return up to 1000 characters of a string
            if (pageText.Length > 1000)
            {
                pageText = pageText.Substring(0, 1000);
            }
            return pageText;
        }

        public bool UrlAllowed(string url)
        {
            foreach (string dis in disallowedUrls)
            {
                if (url.Contains(dis))
                {
                    return false;
                }
            }
            return true;
        }

        public bool tableContains(string url)
        {
            TableQuery<pageData> query = new TableQuery<pageData>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, HttpUtility.UrlEncode(url)));
            if (query == null)
            {
                return false;
            }
            foreach (pageData entity in tableRef.ExecuteQuery(query))
            {
                if (entity.url == url)
                {
                    return true;
                }
            }
            return false;
        }

        //Begin crawling
        public void startCrawl()
        {
            string url = "http://www.cnn.com";
            string url2 = "http://bleacherreport.com";
            queueRef2.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage(url);
            CloudQueueMessage message2 = new CloudQueueMessage(url2);
            queueRef2.AddMessage(message);
            queueRef2.AddMessage(message2);

            crawlerStatus = 1;
            updateDash();
        }

        //Stop crawling
        public void stopCrawl()
        {
            crawlerStatus = 0;
            updateDash();
        }

        //Empty the url queue, command queue, and error list
        public void clearIndex()
        {
            queueRef = queueClient.GetQueueReference("myqueue");
            queueRef.CreateIfNotExists();
            queueRef.Clear();

            queueRef2 = queueClient.GetQueueReference("tickets");
            queueRef2.CreateIfNotExists();
            queueRef2.Clear();

            errList = "";

            crawlerStatus = -1;

            startSitemap();
            updateDash();
        }
        //Empty the table with Webpage data
        public void clearTable()
        {
            tableRef = tableClient.GetTableReference("pageData");
            tableRef.Delete();

            countTable = 0;

            crawlerStatus = -1;

            updateDash();
        }

        //Get the String status of the crawler
        public static string getCrawlerStatus()
        {
            if (crawlerStatus == -1) //Empty of url, sitemap, and table date
            {
                return "Loading";
            }
            else if (crawlerStatus == 0) //Currently stopped, url queue is empty, sitemap and table data remain intact
            {
                return "Idle";
            }
            else  //Currently crawling, url queue is not empty
            {
                return "Crawling";
            } 
        }

        //Method to update the dashboard table with current statistics
        public void updateDash()
        {
            queueRef2.FetchAttributes();
            int? cachedMessageCount = queueRef2.ApproximateMessageCount; // Retrieve the cached approximate message count.

            string tenString = "";
            foreach (string t in lTen)
            {
                tenString += t + ",";
            }
            if (tenString.Length > 0)
            {
                tenString = tenString.Substring(0, tenString.Length - 1);
            }
            if (errList.Length > 0)
            {
                if (errList.StartsWith("$")) //Since we insert '$' at beginning to seperate entries within string
                {
                    errList = errList.Substring(1);
                }
            }

            TableOperation retrieveOperation = TableOperation.Retrieve<dashStat>("Counter", "Key");
            TableResult retrievedResult = dashInfo.Execute(retrieveOperation);
            dashStat updateEntity = (dashStat)retrievedResult.Result;

            updateEntity.wrState = getCrawlerStatus();
            updateEntity.ramUse = "" + RAM.NextValue() + " MB";
            updateEntity.cpuUse = "" + CPU.NextValue() + "%";
            updateEntity.crawled = countCrawl;
            updateEntity.lastTen = tenString;
            updateEntity.qSize = cachedMessageCount ?? default(int);
            updateEntity.tSize = countTable;
            updateEntity.errors = errList;

            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateEntity);
            dashInfo.Execute(insertOrReplaceOperation);
        }
    }

}
