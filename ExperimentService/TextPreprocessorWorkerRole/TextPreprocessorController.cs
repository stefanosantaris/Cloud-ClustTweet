using MyWebRole;
using MyWebRole.Connectors;
using MyWebRole.Model;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextPreprocessorWorkerRole.Preprocessors;

namespace TextPreprocessorWorkerRole
{
    class TextPreprocessorController
    {
        private string parsedJsonFile;
        private string purpose;
        private ArrayList preprocessedTweetList;
        public TextPreprocessorController(string file, string purpose)
        {
            this.parsedJsonFile = file;
            this.purpose = purpose;
            DownloadJsonFile();
        }


        private void DownloadJsonFile()
        {
            BlobConnector blobConnector;
            if (purpose.Equals("training"))
            {
                blobConnector = new BlobConnector(Properties.TrainingParsedContainerName);
            }
            else
            {
                blobConnector = new BlobConnector(Properties.TestingParsedContainerName);
            }

            blobConnector.DownloadFile(parsedJsonFile);
        }

        public void StartPreprocessing()
        {
            preprocessedTweetList = new ArrayList();
            TextReader reader = new StreamReader(parsedJsonFile, Encoding.UTF8);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("["))
                {
                    line = line.Remove(0, 1);
                }
                if (line.EndsWith("]"))
                {
                    line = line.Remove(line.Length - 1, 1);
                }
                if (line.EndsWith(","))
                {
                    line = line.Remove(line.Length - 1, 1);
                }
                ParsedTweet tweet = (ParsedTweet)JsonConvert.DeserializeObject<ParsedTweet>(line);


                #region preprocessing area

                TweetPreprocessor tweetPreprocessor = new TweetPreprocessor(tweet);
                tweetPreprocessor.PreprocessTweet();
                if (tweetPreprocessor.tweet == null)
                {
                    continue;
                }

                if (tweetPreprocessor.tweet.blogs != null)
                {
                    BlogPreprocessor blogProcessor = new BlogPreprocessor(tweetPreprocessor.tweet.blogs);
                    blogProcessor.PreprocessBlogs();
                    tweetPreprocessor.tweet.blogs = blogProcessor.blogs;
                }

                string preprocessedJson = JsonConvert.SerializeObject(tweetPreprocessor.tweet);
                preprocessedTweetList.Add(preprocessedJson);
                #endregion
            }

            reader.Close();

            UploadFile();
            Trace.WriteLine("Worker Role with file " + parsedJsonFile + " is ended");
        }


        private void UploadFile()
        {

            if (preprocessedTweetList.Count != 0)
            {
                TextWriter writer = new StreamWriter(parsedJsonFile);
                writer.Write('[');
                for (int i = 0; i < preprocessedTweetList.Count; i++)
                {
                    if (i == (preprocessedTweetList.Count - 1))
                    {
                        writer.WriteLine(preprocessedTweetList[i] + "]");
                    }
                    else
                    {
                        writer.WriteLine(preprocessedTweetList[i] + ",");
                    }
                }
                writer.Close();

                BlobConnector blobConnector;
                if (purpose.Equals("training"))
                {
                    blobConnector = new BlobConnector(Properties.TrainingPreprocessedContainerName);
                }
                else
                {
                    blobConnector = new BlobConnector(Properties.TestingPreprocessedContainerName);
                }
                blobConnector.UploadFile(parsedJsonFile);
                File.Delete(parsedJsonFile);
            }

        }
    }
}
