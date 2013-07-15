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

namespace IndexWorkerRole
{
    public class IndexController
    {
        private string id, purpose, experimentName;
        private int dimensionalityRate;
        private List<string> indexFilesList;
        private IndexHandler indexHandler;
        private BlobConnector blobConnector;
        public IndexController(string id, string experimentName, string purpose, int dimensionalityRate, string indexFilesJson)
        {
            this.id = id;
            this.experimentName = experimentName;
            this.purpose = purpose;
            this.dimensionalityRate = dimensionalityRate;
            indexFilesList = (List<string>)JsonConvert.DeserializeObject<List<string>>(indexFilesJson);
            indexHandler = new IndexHandler(experimentName, dimensionalityRate);
            if (purpose.Equals("training"))
            {
                blobConnector = new BlobConnector(Properties.TrainingPreprocessedContainerName);
            }
            else
            {
                blobConnector = new BlobConnector(Properties.TestingPreprocessedContainerName);
            }
        }

        public void startIndexing()
        {

            if (purpose.Equals("training"))
            {
                indexFiles();
                QueueConnector queueConnector = new QueueConnector(Properties.IndexFinishQueueName);
                BrokeredMessage message = new BrokeredMessage();
                message.Properties["id"] = id;
                queueConnector.SendMessage(message);
            }
            else
            {
                ClusterFiles();
                string clusteringFileName = string.Empty;
                string[] experimentNameSplit = experimentName.Split(' ');
                for (int i = 0; i < experimentNameSplit.Length; i++)
                {
                    if (i == (experimentNameSplit.Length - 1))
                    {
                        clusteringFileName += experimentNameSplit[i];
                    }
                    else
                    {
                        clusteringFileName += experimentNameSplit[i] + "%20";
                    }
                }
                QueueConnector queueConnector = new QueueConnector(Properties.IndexFinishQueueName);
                BrokeredMessage message = new BrokeredMessage();
                message.Properties["id"] = id;
                message.Properties["clusterUri"] = "http://experinments.blob.core.windows.net/" + Properties.ClusteringContainerName + "/" + clusteringFileName;
                queueConnector.SendMessage(message);
            }
        }

        private void ClusterFiles()
        {
            
            QueueConnector queueConnector = new QueueConnector(Properties.ClusterStartQueueName);
            foreach (string file in indexFilesList)
            {
                blobConnector.DownloadFile(file);
                ArrayList itemRepresantationList = new ArrayList();
                TextReader reader = new StreamReader(file, Encoding.UTF8);
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

                    ItemRepresentation item = indexHandler.GetItemRepresentation(tweet, dimensionalityRate);
                    string itemRepresentationJson = JsonConvert.SerializeObject(item);
                    itemRepresantationList.Add(itemRepresentationJson);
                }
                reader.Close();


                TextWriter writer = new StreamWriter(file);
                writer.Write('[');
                for (int i = 0; i < itemRepresantationList.Count; i++)
                {
                    if (i == (itemRepresantationList.Count - 1))
                    {
                        writer.WriteLine(itemRepresantationList[i] + "]");
                    }
                    else
                    {
                        writer.WriteLine(itemRepresantationList[i] + ",");
                    }
                }
                writer.Close();

                BlobConnector clusterBlobConnector = new BlobConnector(Properties.ClusterItemRepresantationContainername);
                clusterBlobConnector.UploadFile(file);
                string id = DateTime.UtcNow.ToFileTimeUtc().ToString();
                BrokeredMessage message = new BrokeredMessage();
                message.Properties["id"] = id;
                message.Properties["clusterFile"] = experimentName;
                message.Properties["clusteritem"] = file;
                queueConnector.SendMessage(message);
                WaitToCluster(id);
            }

        }

        private void WaitToCluster(string id)
        {
            QueueConnector queueConnector = new QueueConnector(Properties.ClusterFinishQueueName);
            bool exitFlag = false;
            while (!exitFlag)
            {
                BrokeredMessage message = null;
                message = queueConnector.ReceiveMessage();
                if (message != null)
                {
                    string returnId = message.Properties["id"].ToString();
                    if (returnId.Equals(id))
                    {
                        message.Complete();
                        exitFlag = true;
                    }
                    else
                    {
                        message.Abandon();
                    }
                }
            }
        }

        private void indexFiles()
        {
            foreach (string file in indexFilesList)
            {
                blobConnector.DownloadFile(file);
                TextReader reader = new StreamReader(file, Encoding.UTF8);
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
                    indexHandler.IndexTweet(tweet);
                }
                reader.Close();

                File.Delete(file);

                //TextReader reader = new StreamReader(file, Encoding.UTF8);
                //string json = reader.ReadLine();
                //reader.Close();
                //File.Delete(file);
                //List<ParsedTweet> tweetList = (List<ParsedTweet>)JsonConvert.DeserializeObject<List<ParsedTweet>>(json);
                //foreach (ParsedTweet tweet in tweetList)
                //{
                //    indexHandler.IndexTweet(tweet);
                //}

            }

            indexHandler.ImplementTfIdfArray();

            if (dimensionalityRate < 100)
            {
                indexHandler.CalculateSvd();
            }
            indexHandler.SaveProperties();
        }

    }
}
