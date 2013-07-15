using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyWebRole.Models
{
    public class ExperimentModels : TableEntity
    {
        public ExperimentModels(string id, string experimentName, string trainingFile, string trainingFileLink, string testingFile, string testingFileLink, int dimensionality, string startTime, string stopTime, string clusterFile)
        {
            this.PartitionKey = id;
            this.RowKey = experimentName;
            this.id = id;
            this.experimentName = experimentName;
            this.trainingFile = trainingFile;
            this.trainingFileLink = trainingFileLink;
            this.testingFile = testingFile;
            this.testingFileLink = testingFileLink;
            this.dimensionality = dimensionality;
            this.startTime = startTime;
            this.stopTime = stopTime;
            this.clusterFile = clusterFile;
        }

        public ExperimentModels()
        {
        }

        public string id { get; set; }
        public string experimentName { get; set; }
        public string trainingFile { get; set; }
        public string trainingFileLink { get; set; }
        public string testingFile { get; set; }
        public string testingFileLink { get; set; }
        public int dimensionality { get; set; }
        public string startTime { get; set; }
        public string stopTime { get; set; }
        public string clusterFile { get; set; }

    }
}