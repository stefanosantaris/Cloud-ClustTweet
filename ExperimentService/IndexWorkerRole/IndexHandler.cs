using MathNet.Numerics.LinearAlgebra.Double;
using MyWebRole;
using MyWebRole.Connectors;
using MyWebRole.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace IndexWorkerRole
{
    public class IndexHandler
    {
        private IndexProperties properties;
        private string indexFileName;
        private BlobConnector indexBlobConnector;
        private int dimensionalityRate;
        public IndexHandler(string indexFileName, int dimensionalityRate)
        {
            this.dimensionalityRate = dimensionalityRate;
            this.indexFileName = indexFileName;
            indexBlobConnector = new BlobConnector(Properties.IndexContainerName);
            DownloadIndex();
            IntializeProperties();
        }

        private void DownloadIndex()
        {
            if (indexBlobConnector.BlobExist(indexFileName))
            {
                indexBlobConnector.DownloadFile(indexFileName);
            }
        }

        private void UploadIndex()
        {
            indexBlobConnector.UploadFile(indexFileName);
        }

        private void IntializeProperties()
        {
            if (!File.Exists(indexFileName))
            {
                properties = new IndexProperties();
            }
            else
            {
                FileStream fs = new FileStream(indexFileName, FileMode.Open);
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    properties = (IndexProperties)formatter.Deserialize(fs);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason : " + e.Message);
                    properties = new IndexProperties();
                }
                finally
                {
                    fs.Close();
                }
            }
        }

        public void IndexTweet(ParsedTweet tweet)
        {
            string text = tweet.text;
            string[] tokenizedText = text.Split(' ');
            foreach (string token in tokenizedText)
            {
                properties.AddTermDocument(token, tweet.id);
            }
        }


        public void ImplementTfIdfArray()
        {
            int numOfTerms = properties.GetNumOfTerms();
            int numOfDocuments = properties.GetNumOfDocuments();
            double[,] tfArray = new double[numOfTerms, numOfDocuments];
            Dictionary<IndexWorkerRole.IndexProperties.TermDocument, int> tfIdfDictionary = properties.GetTfDictionary();
            foreach (KeyValuePair<IndexWorkerRole.IndexProperties.TermDocument, int> tfValuePair in tfIdfDictionary)
            {
                IndexWorkerRole.IndexProperties.TermDocument termDocumentStruct = tfValuePair.Key;
                int row = properties.GetTermIdentifier(termDocumentStruct.term);
                int column = properties.GetDocumentIdentifier(termDocumentStruct.document);
                int value = tfValuePair.Value;

                tfArray[row, column] += value;


            }
            double[] gfi = CalculateGfi(tfArray);
            double[] gi = CalculateGi(tfArray, gfi);
            tfArray = NormalizeTfArray(tfArray, gi);


            properties.SetTfIdfArray(tfArray);
            properties.SetGfiArray(gfi);
            properties.SetGiArray(gi);

        }

        private double[,] NormalizeTfArray(double[,] tfArray, double[] gi)
        {
            for (int i = 0; i < properties.GetNumOfTerms(); i++)
            {
                for (int j = 0; j < properties.GetNumOfDocuments(); j++)
                {
                    tfArray[i, j] = (float)(gi[i] * Math.Log(tfArray[i, j] + 1));
                }
            }
            return tfArray;
        }

        private double[] CalculateGi(double[,] tfArray, double[] gfi)
        {
            double[] gi = new double[properties.GetNumOfTerms()];
            for (int i = 0; i < properties.GetNumOfTerms(); i++)
            {
                float sum = 0;
                for (int j = 0; j < properties.GetNumOfDocuments(); j++)
                {
                    double pi = tfArray[i, j] / gfi[i];
                    float log = 0;
                    if (pi != 0)
                    {
                        log = (float)((pi * Math.Log(pi)) / Math.Log(properties.GetNumOfDocuments()));
                    }
                    sum += log;
                }
                gi[i] = 1 + sum;
            }

            return gi;
        }

        private double[] CalculateGfi(double[,] tfArray)
        {
            double[] gfi = new double[properties.GetNumOfTerms()];
            for (int i = 0; i < properties.GetNumOfTerms(); i++)
            {
                double sum = 0;
                for (int j = 0; j < properties.GetNumOfDocuments(); j++)
                {
                    sum += tfArray[i, j];
                }
                gfi[i] = sum;
            }
            return gfi;
        }


        public void SaveProperties()
        {
            FileStream fs = new FileStream(indexFileName, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, properties);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason : " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
            UploadIndex();
        }

        public ItemRepresentation GetItemRepresentation(ParsedTweet tweet, int dimensionalityRate)
        {

            ItemRepresentation item = null;
            float[] vectorValue = new float[properties.GetNumOfTerms()];


            string text = tweet.text;
            string[] tokenizedText = text.Split(' ');
            foreach (string token in tokenizedText)
            {
                if (properties.TermExists(token))
                {
                    vectorValue[properties.GetTermIdentifier(token)] += 1;
                }
            }

            for (int i = 0; i < properties.GetNumOfTerms(); i++)
            {
                vectorValue[i] = (float)(properties.gi[i] * Math.Log(vectorValue[i] + 1));
            }

            if (dimensionalityRate < 100)
            {
                double[,] queryDoubleArray = new double[1, properties.GetNumOfTerms()];
                Matrix queryMatrix = DenseMatrix.OfArray(queryDoubleArray);
                for (int i = 0; i < properties.GetNumOfTerms(); i++)
                {
                    queryMatrix[0, i] = vectorValue[i];
                }

                Matrix wkMatrix = DenseMatrix.OfArray(properties.getwMatrixReduced());
                Matrix uMatrix = DenseMatrix.OfArray(properties.getuMatrixReduced());

                var qt_uk = queryMatrix.Multiply(uMatrix);
                var finalQuery = qt_uk.Multiply(wkMatrix);

                double[,] tempQueryArray = finalQuery.ToArray();
                float[] finalQueryArray = new float[finalQuery.ColumnCount];
                for (int i = 0; i < finalQuery.ColumnCount; i++)
                {
                    finalQueryArray[i] = (float)tempQueryArray[0, i];
                }
                item = new ItemRepresentation(tweet.id, finalQueryArray, true);
            }
            else
            {
                item = new ItemRepresentation(tweet.id, vectorValue, true);
            }

            return item;
        }


        public void CalculateSvd()
        {
            DenseMatrix initialMatrix = DenseMatrix.OfArray(properties.GetTfIdfArray());
            var svd = initialMatrix.Svd(true);

            //InitialMatrix = USVT

            double[,] uMatrix = svd.U().ToArray();

            //This matrix contains the singular values as a diagonal matrix
            double[,] wMatrix = svd.W().ToArray();

            double[,] vTransposeMatrx = svd.VT().ToArray();

            int svd_rank = (int)(properties.GetNumOfDocuments() * ((double)dimensionalityRate / 100));

            if (svd_rank == 0 || svd_rank > properties.GetNumOfDocuments())
            {
                svd_rank = properties.GetNumOfDocuments();
            }

            double[,] wMatrixReduced = new double[svd_rank, svd_rank];
            for (int i = 0; i < svd_rank; i++)
            {
                for (int j = 0; j < svd_rank; j++)
                {
                    wMatrixReduced[i, j] = wMatrix[i, j];
                }
            }

            double[,] wMatrixReducedInverse = DenseMatrix.OfArray(wMatrixReduced).Inverse().ToArray();

            double[,] uMatrixReduced = new double[properties.GetNumOfTerms(), svd_rank];
            for (int i = 0; i < properties.GetNumOfTerms(); i++)
            {
                for (int j = 0; j < svd_rank; j++)
                {
                    uMatrixReduced[i, j] = uMatrix[i, j];
                }
            }

            properties.setwMatrixReduced(wMatrixReducedInverse);
            properties.setuMatrixReduced(uMatrixReduced);


        }
    }
}
