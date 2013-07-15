using JsonParserWorkerRole.Network;
using MyWebRole.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParserWorkerRole
{
    class Parser
    {
        private string json;

        public Parser(string json)
        {
            this.json = json;
        }

        public ParsedTweet ParseJson()
        {
            JObject jsonObject = (JObject)JsonConvert.DeserializeObject(json);

            JsonObjectHandler jsonHandler = new JsonObjectHandler(jsonObject);

            if (jsonHandler.source.Value.ToString().Contains("foursquare"))
            {
                return null;
            }

            if (jsonHandler.hashtagsArray.Count != 0)
            {
                jsonHandler.processHashTags();
            }

            ParsedBlog[] blogsArray = null;
            if (jsonHandler.urlsArray.Count != 0)
            {
                UrlHandler urlManager = new UrlHandler(jsonHandler.urlsArray, jsonHandler.text);
                urlManager.processUrls();
                if (urlManager.blogList.Count != 0)
                {
                    blogsArray = urlManager.blogList.ToArray();
                }
                jsonHandler.text = urlManager.text;
            }

            string[] hashtagsTextArray = null;
            if (jsonHandler.hashtagsArray.Count != 0)
            {
                hashtagsTextArray = (string[])jsonHandler.hashtagTexts.ToArray(typeof(string));
            }


            ParsedTweet tweet = new ParsedTweet(jsonHandler.id, jsonHandler.text, jsonHandler.createdDateTime, jsonHandler.geolon, jsonHandler.geolat, jsonHandler.place, hashtagsTextArray, jsonHandler.userId, blogsArray);

            return tweet;
        }
    }
}
