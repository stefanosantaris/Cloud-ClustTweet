﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using MyWebRole;
using TextPreprocessorWorkerRole.Database;
using MyWebRole.Connectors;

namespace TextPreprocessorWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue
        const string QueueName = Properties.PreprocessorQueueName;

        public static Dictionary<string, string> slangDictionary = new Dictionary<string, string>();
        public static string fileName;
        public static QueueConnector queueSendTokenConnector, queueReceiveTokenConnector;

        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        QueueClient Client;
        bool IsStopped;

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
                        string file = receivedMessage.Properties["parseFile"].ToString();
                        string purpose = receivedMessage.Properties["purpose"].ToString();
                        Trace.WriteLine("Processing", receivedMessage.SequenceNumber.ToString());
                        Trace.WriteLine("Worker Role with file " + file + " is starting");
                        receivedMessage.Complete();
                        fileName = file;
                        TextPreprocessorController preprocessorController = new TextPreprocessorController(file, purpose);
                        preprocessorController.StartPreprocessing();
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

            queueSendTokenConnector = new QueueConnector(Properties.SpellTokenSendQueueName);
            queueReceiveTokenConnector = new QueueConnector(Properties.SpellTokenReceiveQueueName);

            DBConnect databaseController = new DBConnect();
            databaseController.SelectAll();


            BlobConnector blobConnector = new BlobConnector(Properties.StopListContainerName);
            blobConnector.DownloadFile("StopList.txt");

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