using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyWebRole
{
    public static class Properties
    {
        //These strings contain the container name where the initial files with the json ids are stored
        public static readonly string TrainingIdFilesContainerName = "trainingidfiles";
        public static readonly string TestingIdFilesContainerName = "testingidfiles";

        public static readonly string TrainingParsedContainerName = "trainingparsedfiles";
        public static readonly string TestingParsedContainerName = "testingparsedfiles";

        //These strings contain the container name where the proprocessed json files are stored
        public static readonly string TrainingPreprocessedContainerName = "trainingpreprocessed";
        public static readonly string TestingPreprocessedContainerName = "testingpreprocessed";

        public static readonly string IndexContainerName = "indexfiles";

        public static readonly string ClusteringContainerName = "clusteringfiles";

        public static readonly string StopListContainerName = "stoplist";

        public static readonly string ClusterItemRepresantationContainername = "clusteritemrepresantationfiles";


        public const string ExperimentsInitializationQueueName = "ExperimentsQueue";
        public const string ParserQueueName = "ParserQueue";
        public const string PreprocessorQueueName = "PreprocessorQueue";
        public const string SpellTokenSendQueueName = "SpellTokenSendQueue";
        public const string SpellTokenReceiveQueueName = "SpellTokenReceiveQueue";
        public const string IndexStartQueueName = "IndexStartQueue";
        public const string IndexFinishQueueName = "IndexFinishQueue";
        public const string ClusterStartQueueName = "ClusterStartQueue";
        public const string ClusterFinishQueueName = "ClusterFinishQueue";

        public static readonly string DatasetConnectionString = CloudConfigurationManager.GetSetting("Microsoft.DatabaseStorage.ConnectionString");
        public static readonly string DatasetContainerName = "cross";
    }
}