using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MyWebRole;
using Newtonsoft.Json;
using MyWebRole.Connectors;
using MyWebRole.Model;
using System.Text;
using System.IO;

namespace ClusteringWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue
        const string QueueName = Properties.ClusterStartQueueName;
        private string clusterFileName = string.Empty;
        private ClusterHandler clusterManager;
        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        QueueClient Client;
        bool IsStopped;
        private QueueConnector queueConnector;

        public override void Run()
        {
            while (!IsStopped)
            {
                try
                {
                    // Receive the message
                    BrokeredMessage receivedMessage = null;
                    receivedMessage = Client.Receive();

                    if (receivedMessage != null)
                    {
                        string tempClusterFileName = receivedMessage.Properties["clusterFile"].ToString();
                        string clusterItemRepresentationFile = receivedMessage.Properties["clusteritem"].ToString();
                        string id = receivedMessage.Properties["id"].ToString();
                        // Process the message
                        Trace.WriteLine("Processing", receivedMessage.SequenceNumber.ToString());
                        receivedMessage.Complete();
                        receivedMessage = null;

                        if (!clusterFileName.Equals(tempClusterFileName))
                        {
                            clusterFileName = tempClusterFileName;
                            clusterManager = new ClusterHandler(clusterFileName);
                        }

                        BlobConnector clusterItemBlobConnector = new BlobConnector(Properties.ClusterItemRepresantationContainername);
                        clusterItemBlobConnector.DownloadFile(clusterItemRepresentationFile);

                        TextReader reader = new StreamReader(clusterItemRepresentationFile, Encoding.UTF8);
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
                            ItemRepresentation item = (ItemRepresentation)JsonConvert.DeserializeObject<ItemRepresentation>(line);
                            clusterManager.AssignItemToCluster(item);
                        }
                        reader.Close();
                        clusterManager.SaveClusters();

                        //ItemRepresentation item = (ItemRepresentation)JsonConvert.DeserializeObject<ItemRepresentation>(itemRepresentationJson);
                        //clusterManager.AssignItemToCluster(item);
                        //clusterManager.SaveClusters();


                        QueueConnector queueConnector = new QueueConnector(Properties.ClusterFinishQueueName);
                        BrokeredMessage sendMessage = new BrokeredMessage();
                        sendMessage.Properties["id"] = id;
                        queueConnector.SendMessage(sendMessage);
                    }
                }
                catch (MessagingException e)
                {
                    if (!e.IsTransient)
                    {
                        Trace.WriteLine(e.Message);
                        throw;
                    }

                    Thread.Sleep(10000);
                }
                catch (OperationCanceledException e)
                {
                    if (!IsStopped)
                    {
                        Trace.WriteLine(e.Message);
                        throw;
                    }
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;
            QueueDescription qd = new QueueDescription(QueueName);
            qd.MaxSizeInMegabytes = 5120;
            qd.LockDuration = new TimeSpan(0, 5, 0);
            qd.MaxDeliveryCount = 100;
            // Create the queue if it does not exist already
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(qd);
            }
            queueConnector = new QueueConnector(Properties.ClusterFinishQueueName);

            // Initialize the connection to Service Bus Queue
            Client = QueueClient.CreateFromConnectionString(connectionString, QueueName);
            IsStopped = false;
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            IsStopped = true;
            Client.Close();
            base.OnStop();
        }
    }
}
