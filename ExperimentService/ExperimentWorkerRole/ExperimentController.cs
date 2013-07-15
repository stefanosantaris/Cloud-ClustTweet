using Microsoft.ServiceBus.Messaging;
using MyWebRole;
using MyWebRole.Connectors;
using MyWebRole.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentWorkerRole
{
    class ExperimentController
    {
        private string trainingFile, testingFile, experimentName, clusterUri;
        private int dimensionalityRate;
        private ExperimentModels model;
        public ExperimentController(ExperimentModels model)
        {
            this.model = model;
            this.trainingFile = model.trainingFile.Split('/').Last();
            this.testingFile = model.testingFile.Split('/').Last();
            this.experimentName = model.experimentName;
            this.dimensionalityRate = model.dimensionality;
        }

        public void StartExperiment()
        {
            DownloadFiles();
            List<string> trainingFilesList = SplitFiles(trainingFile, true);
            List<string> blobTrainingFiles = UploadFiles(trainingFilesList, Properties.TrainingIdFilesContainerName);
            List<string> testingFilesList = SplitFiles(testingFile, false);
            List<string> blobTestingFiles = UploadFiles(testingFilesList, Properties.TestingIdFilesContainerName);
            SendFilesForProcessing(blobTrainingFiles, true);
            SendFilesForProcessing(blobTestingFiles, false);
            StartTraining(blobTrainingFiles);
            StartTesting(blobTestingFiles);
        }

        private void SendFilesForProcessing(List<string> blobFiles, bool training)
        {
            QueueConnector queue = new QueueConnector(Properties.ParserQueueName);

            if (training)
            {
                foreach (string file in blobFiles)
                {
                    BrokeredMessage message = new BrokeredMessage();
                    message.Properties["parseFile"] = file;
                    message.Properties["purpose"] = "training";
                    queue.SendMessage(message);
                }
            }
            else
            {
                foreach (string file in blobFiles)
                {
                    BrokeredMessage message = new BrokeredMessage();
                    message.Properties["parseFile"] = file;
                    message.Properties["purpose"] = "testing";
                    queue.SendMessage(message);
                }
            }
        }

        private void StartTesting(List<string> blobTestingFiles)
        {


            bool exitFlag = false;
            BlobConnector blobConnector = new BlobConnector(Properties.TestingPreprocessedContainerName);
            while (!exitFlag)
            {
                bool existAll = true;
                foreach (string blob in blobTestingFiles)
                {
                    if (!blobConnector.BlobExist(blob))
                    {
                        existAll = false;
                    }
                }

                if (existAll)
                {
                    exitFlag = true;
                }
                else
                {
                    Thread.Sleep(10000);
                }
            }

            StartIndexing(blobTestingFiles, "testing");

            TableConnector tableConnector = new TableConnector();
            tableConnector.UpdateExperiments(model, clusterUri, DateTime.UtcNow);
        }

        private void StartTraining(List<string> blobFiles)
        {
            bool exitFlag = false;
            BlobConnector blobConnector = new BlobConnector(Properties.TrainingPreprocessedContainerName);
            while (!exitFlag)
            {
                bool existAll = true;
                foreach (string blob in blobFiles)
                {
                    if (!blobConnector.BlobExist(blob))
                    {
                        existAll = false;
                    }
                }

                if (existAll)
                {
                    exitFlag = true;
                }
                else
                {
                    Thread.Sleep(10000);
                }
            }

            StartIndexing(blobFiles, "training");
        }

        private void StartIndexing(List<string> blobFiles, string purpose)
        {
            string indexFilesJson = JsonConvert.SerializeObject(blobFiles);
            string id = DateTime.UtcNow.ToFileTimeUtc().ToString();
            QueueConnector indexStartConnector = new QueueConnector(Properties.IndexStartQueueName);
            BrokeredMessage brokeredMessage = new BrokeredMessage();
            brokeredMessage.Properties["id"] = id;
            brokeredMessage.Properties["experimentName"] = experimentName;
            brokeredMessage.Properties["indexFiles"] = indexFilesJson;
            brokeredMessage.Properties["dimensionalityRate"] = dimensionalityRate;
            brokeredMessage.Properties["purpose"] = purpose;
            indexStartConnector.SendMessage(brokeredMessage);

            QueueConnector indexFinishConnector = new QueueConnector(Properties.IndexFinishQueueName);
            bool exitFlag = false;
            while (!exitFlag)
            {
                BrokeredMessage message = null;
                message = indexFinishConnector.ReceiveMessage();
                if (message != null)
                {
                    string receivedId = message.Properties["id"].ToString();
                    if (receivedId.Equals(id))
                    {
                        if (purpose.Equals("testing"))
                        {
                            clusterUri = message.Properties["clusterUri"].ToString();
                        }
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

        private List<string> UploadFiles(List<string> filesList, string container)
        {
            List<String> blobFiles = new List<string>();
            BlobConnector blobConnector = new BlobConnector(container);
            foreach (string file in filesList)
            {
                string uri = blobConnector.UploadFile(file);
                blobFiles.Add(uri.Split('/').Last());
                File.Delete(file);
            }
            return blobFiles;
        }


        private void DownloadFiles()
        {
            BlobConnector blobConnector = new BlobConnector(Properties.TrainingIdFilesContainerName);
            trainingFile = blobConnector.DownloadFile(trainingFile);

            blobConnector = new BlobConnector(Properties.TestingIdFilesContainerName);
            testingFile = blobConnector.DownloadFile(testingFile);
        }

        private List<string> SplitFiles(string file, bool training)
        {
            List<string> filesList = new List<string>();
            string path = string.Empty;
            string currentPath = Directory.GetCurrentDirectory();
            if (training)
            {
                path = currentPath + @"\training\";
            }
            else
            {
                path = currentPath + @"\testing\";
            }



            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = file.Split('\\').Last();

            TextReader reader = new StreamReader(file);
            string line;
            int counter = 0;
            int numFiles = -1;
            TextWriter writer = new StreamWriter(path + fileName + "_" + (++numFiles));
            filesList.Add(path + fileName + "_" + numFiles);
            while ((line = reader.ReadLine()) != null)
            {
                writer.WriteLine(line);
                if (((++counter) % 100) == 0)
                {
                    writer.Close();
                    writer = new StreamWriter(path + "\\" + fileName + "_" + (++numFiles));
                    filesList.Add(path + "\\" + fileName + "_" + numFiles);
                }

            }
            writer.Close();
            reader.Close();

            List<string> finalList = new List<string>();
            foreach (string testFile in filesList)
            {
                FileInfo info = new FileInfo(testFile);
                if (info.Length > 0)
                {
                    finalList.Add(testFile);
                }
                else
                {
                    File.Delete(testFile);
                }
            }

            return finalList;
        }
    }
}
