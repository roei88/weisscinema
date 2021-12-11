using System;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Google.Protobuf;

namespace Main
{
    // attribute for classes that are handling the Google Protobuf messages
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageHandlerClassAttribute : Attribute
    {
    }

    // attribute for methods that are handling the new-format messages 
    // (messages that arrive as nested messages inside the WrapperMessage)
    [AttributeUsage(AttributeTargets.Method)]
    public class NewFormatMessageHandlerMethodAttribute : Attribute
    {
    }

    [MessageHandlerClass]
    public class GeneralMessageHandler
    {
        //TODO:: move to DB/config file
        const string OMDB_API_KEY = "bb182d9e";
        const string OMDB_URL = @"http://www.omdbapi.com/?";
        const int MAX_PAGES = 10;
        const int PAGE_SIZE = 10;
        protected MessageHandlersManager _messageHandlerManager;

        public GeneralMessageHandler(MessageHandlersManager messageHandlerManager)
        {
            _messageHandlerManager = messageHandlerManager;
        }

        protected void Send(IMessage msg)
        {
            _messageHandlerManager.SendWrappedMessage(msg);
        }

        /// <summary>
        /// Actions upon version message from web client
        /// </summary>
        /// <param name="versionMessage"></param>
        [NewFormatMessageHandlerMethod]
        public void HandleVersionMessage(ProtoMessages.VersionMessage versionMessage)
        {
            try
            {
                Console.WriteLine($"Received VersionMessage of type {versionMessage.Version}");

                //TODO:: Check that UI version match and act accordingally

                //HandleTitlesSearch(new TitlesSearch());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse/handle an old-format GeneralMessage, error: " + ex.Message);
            }
        }

        /// <summary>
        /// Produce and send new full ArticlesMessage with data taken from newsapi
        /// </summary>
        /// <param name="articles">Empty articles message</param>
        [NewFormatMessageHandlerMethod]
        public void HandleTitlesSearch(ProtoMessages.TitlesSearch titlesSearch)
        {
            titlesSearch.Search.Clear();
            titlesSearch.Pages.Clear();

            if (!string.IsNullOrEmpty(titlesSearch.SearchQuery))
			{
                Console.WriteLine($"received new TitlesSearch message with query: {titlesSearch.SearchQuery}");
                List<SingleTitle> listOfTitles = new List<SingleTitle>();
                int currentPage = 1;
                int count = 0;
                List<JObject> pages = new List<JObject>();
                var urlBase = $"{OMDB_URL}{GetOMDBApiKey()}";
                var url = $"{urlBase}&{GetQueries(titlesSearch.SearchQuery)}";
                
                try
                {
                    //TODO:: check that result is true
                    JObject titlesObj = JObject.Parse(new WebClient().DownloadString(url));
                    JArray titlesArr = (JArray)titlesObj["Search"];
                    //string pageStr = (string)titlesObj["Search"];
                    if (titlesArr!=null)
					{
                        //titlesSearch.Pages.Add(titlesArr.ToString());
                        titlesSearch.Search.AddRange(titlesArr.ToObject<List<ProtoMessages.SingleTitle>>());
                        count += PAGE_SIZE;
                        double totalResults = double.Parse((string)titlesObj["totalResults"]);
                        double devition = (totalResults / PAGE_SIZE);
                        int totalPages = (int)Math.Ceiling(devition);
                        totalResults -= PAGE_SIZE;

                        while (totalResults > 0 && currentPage < MAX_PAGES)
                        {
                            currentPage++;
                            var nextPageUrl = $"{urlBase}&{GetQueries(titlesSearch.SearchQuery, (currentPage).ToString())}";
                            JArray nextPageArr = (JArray)(JObject.Parse(new WebClient().DownloadString(nextPageUrl))["Search"]);
                            titlesSearch.Search.AddRange(titlesArr.ToObject<List<ProtoMessages.SingleTitle>>());
                            totalResults -= PAGE_SIZE;
                            count += PAGE_SIZE;
                        }

                        if (totalResults < 0)
                        {
                            count += (int)totalResults;
                        }

                        titlesSearch.TotalResults = count;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message} Trace: {ex.StackTrace}");
                }

                Console.WriteLine("Sending results");
            }


            Send(titlesSearch);
        }

        //helper methods for ProduceContentMessage()
        //http://www.omdbapi.com/?apikey=bb182d9e&type=movie&page=2&s=the+mat
        private string GetQueries(string searchQuery = "", string page = "1", string searchType = "movie")
        {
            return $"type={searchType}&page={page}&s={searchQuery}";
        }

        private string GetOMDBApiKey()
        {
            return $"apiKey={OMDB_API_KEY}";
        }
    }



    public class TitlesSearch
    {
        public List<SingleTitle> search;
        public string totalResults;
        public string response;
        public string searchQuery;
    }

    public class SingleTitle
    {
        public string title;
        public string year;
        public string imdbID;
        public string type;
        public string poster;
    }


    //public class articlerecord
    //{
    //    public articlesource source;
    //    public string author;
    //    public string title;
    //    public string description;
    //    public string url;
    //    public string urltoimage;
    //    public string publishedat;
    //    public string content;
    //}

    //public class articlesource
    //{
    //    public string id;
    //    public string name;
    //}
}
