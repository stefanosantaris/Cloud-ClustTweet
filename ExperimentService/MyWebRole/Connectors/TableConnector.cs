using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MyWebRole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyWebRole.Connectors
{
    public class TableConnector
    {
        private CloudTable table;

        public TableConnector()
        {
            Initialize();
        }

        public TableConnector(string tableName)
        {
            Initialize(tableName);
        }


        private void Initialize(string tableName)
        {
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.TableStorage.ConnectionString");
            CloudStorageAccount tableStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = tableStorageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("tableName");
            table.CreateIfNotExists();
        }


        private void Initialize()
        {
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.TableStorage.ConnectionString");
            CloudStorageAccount tableStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = tableStorageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("experimenttable");
            table.CreateIfNotExists();

        }

        public List<ExperimentModels> RetrieveExperiments()
        {
            TableQuery<ExperimentModels> rangeQuery = new TableQuery<ExperimentModels>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "1")
            );

            List<ExperimentModels> retrievedModels = new List<ExperimentModels>();

            foreach (ExperimentModels model in table.ExecuteQuery(rangeQuery))
            {
                retrievedModels.Add(model);
            }
            return retrievedModels;
        }


        public void UpdateExperiments(ExperimentModels model, string clusterUri, DateTime stopExperiment)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<ExperimentModels>(model.PartitionKey, model.RowKey);
            TableResult retrievedResult = table.Execute(retrieveOperation);

            ExperimentModels updateEntity = (ExperimentModels)retrievedResult.Result;

            if (updateEntity != null)
            {
                updateEntity.clusterFile = clusterUri;
                updateEntity.stopTime = stopExperiment.ToFileTimeUtc().ToString();
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateEntity);
                table.Execute(insertOrReplaceOperation);
            }
        }

        public void InsertExperiment(ExperimentModels experiment)
        {
            //string connectionString = CloudConfigurationManager.GetSetting("Microsoft.TableStorage.ConnectionString");
            //CloudStorageAccount tableStorageAccount = CloudStorageAccount.Parse(connectionString);
            //CloudTableClient tableClient = tableStorageAccount.CreateCloudTableClient();
            //table = tableClient.GetTableReference("experimenttable");
            //table.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.Insert(experiment);
            table.Execute(insertOperation);
        }


    }
}