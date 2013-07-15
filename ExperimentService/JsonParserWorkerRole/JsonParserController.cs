using Microsoft.ServiceBus.Messaging;
using MyWebRole;
using MyWebRole.Connectors;
using MyWebRole.Model;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParserWorkerRole
{
    class JsonParserController
    {
        private string jsonFile;
        private string purpose;
        public JsonParserController(string file, string purpose)
        {
            jsonFile = file;
            this.purpose = purpose;
            DownloadFile();
        }

        private void DownloadFile()
        {
            if (purpose.Equals("training"))
            {
                BlobConnector blobConnector = new BlobConnector(Properties.TrainingIdFilesContainerName);
                blobConnector.DownloadFile(jsonFile);
            }
            else
            {
                BlobConnector blobConnector = new BlobConnector(Properties.TestingIdFilesContainerName);
                blobConnector.DownloadFile(jsonFile);
            }


        }

        public void StartParsing()
        {
            BlobConnector blobConnector = new BlobConnector(Properties.DatasetContainerName, Properties.DatasetConnectionString);
            ArrayList writeList = new ArrayList();
            TextReader reader = new StreamReader(jsonFile);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] splitLine = line.Split('\t');
                string tweetId = splitLine.First();
                blobConnector.DownloadFile(tweetId + ".txt");
                string json = GetJson(tweetId + ".txt");
                File.Delete(tweetId + ".txt");
                Parser jsonParser = new Parser(json);
                ParsedTweet tweet = jsonParser.ParseJson();
                if (tweet != null)
                {
                    string jsonString = JsonConvert.SerializeObject(tweet);
                    writeList.Add(jsonString);
                }
            }
            reader.Close();

            UploadResults(writeList);
            StartPreprocessing();
        }

        private void StartPreprocessing()
        {
            QueueConnector queue = new QueueConnector(Properties.PreprocessorQueueName);
            BrokeredMessage brokeredMessage = new BrokeredMessage();
            brokeredMessage.Properties["parseFile"] = jsonFile;
            brokeredMessage.Properties["purpose"] = purpose;
            queue.SendMessage(brokeredMessage);
        }


        private void UploadResults(ArrayList writeList)
        {

            TextWriter writer = new StreamWriter(jsonFile);
            writer.Write('[');
            for (int i = 0; i < writeList.Count; i++)
            {
                if (i == (writeList.Count - 1))
                {
                    writer.WriteLine(writeList[i] + "]");
                }
                else
                {
                    writer.WriteLine(writeList[i] + ",");
                }
            }
            writer.Close();
            BlobConnector blobConnector;
            if (purpose.Equals("training"))
            {
                blobConnector = new BlobConnector(Properties.TrainingParsedContainerName);
            }
            else
            {
                blobConnector = new BlobConnector(Properties.TestingParsedContainerName);
            }
            blobConnector.UploadFile(jsonFile);
        }

        private string GetJson(string fileName)
        {
            TextReader reader = new StreamReader(fileName);
            string json = string.Empty;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("<input type=\"hidden\" id=\"init-data\" class=\"json-data\" value=\"{&quot;macawSwift"))
                {
                    int indexOf = line.IndexOf("value=");
                    string jsonLine = line.Substring(indexOf + 7, line.Length - indexOf - 9);
                    jsonLine = jsonLine.Replace("&quot;", "\"");
                    json = jsonLine;
                }
            }
            reader.Close();
            return json;
        }
    }
}
