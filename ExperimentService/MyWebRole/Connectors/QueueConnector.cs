using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyWebRole.Connectors
{
    public class QueueConnector
    {
        private QueueClient experimentQueueClient;

        private string connectionString;

        public QueueConnector(string queueName)
        {
            Initialize(queueName);
        }

        private NamespaceManager CreateNamespaceManager()
        {
            connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

            return NamespaceManager.CreateFromConnectionString(connectionString);
        }


        private void Initialize(string queueName)
        {
            QueueDescription qd = new QueueDescription(queueName);
            qd.MaxSizeInMegabytes = 5120;
            qd.LockDuration = new TimeSpan(0, 5, 0);
            qd.MaxDeliveryCount = 100;
            var namespaceManager = CreateNamespaceManager();


            if (!namespaceManager.QueueExists(queueName))
            {
                namespaceManager.CreateQueue(qd);
            }

            experimentQueueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);
        }


        //public void SendMessage(string message, string trainingFile, string testingFile, string experimentName, int dimensionality)
        //{
        //    BrokeredMessage brokeredMessage = new BrokeredMessage(message);
        //    brokeredMessage.Properties["trainingFile"] = trainingFile;
        //    brokeredMessage.Properties["testingFile"] = testingFile;
        //    brokeredMessage.Properties["experimentName"] = experimentName;
        //    brokeredMessage.Properties["dimensionality"] = dimensionality;
        //    experimentQueueClient.Send(brokeredMessage);
        //}

        //public void SendMessage(string message, string file, string purpose)
        //{
        //    BrokeredMessage brokeredMessage = new BrokeredMessage(message);
        //    brokeredMessage.Properties["parseFile"] = file;
        //    brokeredMessage.Properties["purpose"] = purpose;
        //    experimentQueueClient.Send(brokeredMessage);
        //}

        public void SendMessage(string id, string token)
        {
            BrokeredMessage brokeredMessage = new BrokeredMessage();
            brokeredMessage.Properties["id"] = id;
            brokeredMessage.Properties["token"] = token;
            experimentQueueClient.Send(brokeredMessage);
        }

        public void SendMessage(BrokeredMessage message)
        {
            experimentQueueClient.Send(message);
        }

        //public void SendMessage(string model)
        //{
        //    BrokeredMessage brokeredMessage = new BrokeredMessage();
        //    brokeredMessage.Properties["ExperimentModel"] = model;
        //    experimentQueueClient.Send(brokeredMessage);
        //}

        //public void SendMessage(string id, string indexFilesJson, int dimensionalityRate,string purpose)
        //{
        //    BrokeredMessage brokeredMessage = new BrokeredMessage();
        //    brokeredMessage.Properties["id"] = id;
        //    brokeredMessage.Properties["indexFiles"] = indexFilesJson;
        //    brokeredMessage.Properties["dimensionalityRate"] = dimensionalityRate;
        //    brokeredMessage.Properties["purpose"] = purpose;
        //    experimentQueueClient.Send(brokeredMessage);
        //}

        public BrokeredMessage ReceiveMessage()
        {
            return experimentQueueClient.Receive();
        }
    }
}