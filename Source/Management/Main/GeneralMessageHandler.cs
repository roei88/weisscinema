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
        Dictionary<string, ProtoMessages.MovieTitle> _wishList;

        public GeneralMessageHandler(MessageHandlersManager messageHandlerManager)
        {
            _messageHandlerManager = messageHandlerManager;
            initWishList();
        }

        //TODO:: load wishlist from file/db
        protected bool initWishList()
		{
            _wishList = new Dictionary<string, ProtoMessages.MovieTitle>();

            ProtoMessages.MovieTitle m1 = GetTitle(new ProtoMessages.MovieTitle() { ImdbID = "tt0133093" });
            _wishList.Add("tt0133093", m1);

            ProtoMessages.MovieTitle m2 = GetTitle(new ProtoMessages.MovieTitle() { ImdbID = "tt0289879" });
            _wishList.Add("tt0289879", m2);

            ProtoMessages.MovieTitle m3 = GetTitle(new ProtoMessages.MovieTitle() { ImdbID = "tt0109830" });
            _wishList.Add("tt0109830", m3);

            return true;
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
                HandleTitlesSearch(new ProtoMessages.TitlesSearch()); //for sending the wishlist
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse/handle an old-format GeneralMessage, error: " + ex.Message);
            }
        }

        /// <summary>
        /// Hangles new TitlesSearch message
        /// </summary>
        /// <param name="titlesSearch">TitlesSearch message</param>
        [NewFormatMessageHandlerMethod]
        public void HandleTitlesSearch(ProtoMessages.TitlesSearch titlesSearch)
        {
            titlesSearch.Search.Clear();
            titlesSearch.WishTitles.Clear();

            //whishlist

            //TODO:: save to localfile/db

            if (!string.IsNullOrEmpty(titlesSearch.AddWishTitle) && !_wishList.ContainsKey(titlesSearch.AddWishTitle))
			{
                ProtoMessages.MovieTitle newWishTitle = GetTitle(new ProtoMessages.MovieTitle() { ImdbID = titlesSearch.AddWishTitle });
                _wishList.Add(titlesSearch.AddWishTitle, newWishTitle);
                titlesSearch.AddWishTitle = string.Empty;
            }

            if (!string.IsNullOrEmpty(titlesSearch.RemoveWishTitle) && _wishList.ContainsKey(titlesSearch.RemoveWishTitle))
            {
                _wishList.Remove(titlesSearch.RemoveWishTitle);
                titlesSearch.RemoveWishTitle = string.Empty;
            }

            //TODO:: change to add range with proper casting
            foreach (KeyValuePair<string, ProtoMessages.MovieTitle> pair in _wishList) //init wishlist
            {
                titlesSearch.WishTitles.Add(pair.Key, pair.Value);
            }

            //search

            if (!string.IsNullOrEmpty(titlesSearch.SearchQuery))
			{
                Console.WriteLine($"received new TitlesSearch message with query: {titlesSearch.SearchQuery}");
                int currentPage = 1;
                int count = 0;
                List<JObject> pages = new List<JObject>();
                var urlBase = $"{OMDB_URL}{GetOMDBApiKey()}";
                var url = $"{urlBase}&{GetQueries(titlesSearch.SearchQuery)}";
                
                try
                {
                    JObject titlesObj = JObject.Parse(new WebClient().DownloadString(url));
                    JArray titlesArr = (JArray)titlesObj["Search"];
                    if (titlesArr!=null)
					{
                        titlesSearch.Search.AddRange(titlesArr.ToObject<List<ProtoMessages.SingleTitle>>());
                        count += PAGE_SIZE;
                        double totalResults = double.Parse((string)titlesObj["totalResults"]);
                        double devition = (totalResults / PAGE_SIZE);
                        int totalPages = (int)Math.Ceiling(devition);
                        totalResults -= PAGE_SIZE;

                        while (totalResults > 0 && currentPage < MAX_PAGES) //add  maximum pages allowed
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

        /// <summary>
        /// Habdles new MovieTitle message 
        /// </summary>
        /// <param name="title">MovieTitle message containing the imdb id</param>
        [NewFormatMessageHandlerMethod]
        public void HandleMovieTitle(ProtoMessages.MovieTitle title)
        {
            if (!string.IsNullOrEmpty(title.ImdbID))
            {
                Console.WriteLine($"received new MovieTitle message with imdbID: {title.ImdbID}");

                try
                {
                    if (string.IsNullOrEmpty(title.Response))
                    {
                        title = GetTitle(title);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message} Trace: {ex.StackTrace}");
                }

                Console.WriteLine("Sending results");
            }

            Send(title);
        }


        //TODO:: deserialize rest result into movie title obj
        private ProtoMessages.MovieTitle GetTitle(ProtoMessages.MovieTitle title)
        {
            var url = $"{OMDB_URL}{GetOMDBApiKey()}&i={title.ImdbID}";
            JObject titleObj = JObject.Parse(new WebClient().DownloadString(url));
            title.Title = (string)titleObj["Title"];
            title.Year = (string)titleObj["Year"];
            title.Rated = (string)titleObj["Rated"];
            title.Released = (string)titleObj["Released"];
            title.Runtime = (string)titleObj["Runtime"];
            title.Genre = (string)titleObj["Genre"];
            title.Director = (string)titleObj["Director"];
            title.Writer = (string)titleObj["Writer"];
            title.Actors = (string)titleObj["Actors"];
            title.Plot = (string)titleObj["Plot"];
            title.Language = (string)titleObj["Language"];
            title.Country = (string)titleObj["Country"];
            title.Awards = (string)titleObj["Awards"];
            title.Poster = (string)titleObj["Poster"];
            title.Metascore = (string)titleObj["Metascore"];
            title.ImdbRating = (string)titleObj["imdbRating"];
            title.ImdbVotes = (string)titleObj["imdbVotes"]; ;
            title.Type = (string)titleObj["Type"];
            title.Dvd = (string)titleObj["DVD"];
            title.BoxOffice = (string)titleObj["BoxOffice"];
            title.Production = (string)titleObj["Production"];
            title.Website = (string)titleObj["Website"];
            title.Response = (string)titleObj["Response"];
            JArray ratringArr = (JArray)(titleObj["Ratings"]);
            title.Ratings.AddRange(ratringArr.ToObject<List<ProtoMessages.TitleRating>>());
            return title;
        }

        private string GetQueries(string searchQuery = "", string page = "1", string searchType = "movie")
        {
            return $"type={searchType}&page={page}&s={searchQuery}";
        }

        private string GetOMDBApiKey()
        {
            return $"apiKey={OMDB_API_KEY}";
        }
    }
}
