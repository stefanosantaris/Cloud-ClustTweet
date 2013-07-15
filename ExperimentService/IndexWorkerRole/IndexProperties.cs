using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexWorkerRole
{
    [Serializable]
    public class IndexProperties
    {
        [Serializable]
        public struct TermDocument
        {
            public string term;
            public string document;
        }

        private Dictionary<string, int> termDictionary;
        private Dictionary<string, int> documentDictionary;
        private Dictionary<TermDocument, int> tfDictionary;
        private double[,] tfIdfArray;
        public double[] gfi, gi;
        double[,] wMatrixReduced;
        double[,] uMatrixReduced;


        public IndexProperties()
        {
            termDictionary = new Dictionary<string, int>();
            documentDictionary = new Dictionary<string, int>();
            tfDictionary = new Dictionary<TermDocument, int>();
            wMatrixReduced = new double[1, 1];
            uMatrixReduced = new double[1, 1];

        }


        public void AddTermDocument(string term, string document)
        {
            if (!documentDictionary.ContainsKey(document))
            {
                documentDictionary.Add(document, documentDictionary.Count);
            }


            if (!termDictionary.ContainsKey(term))
            {
                termDictionary.Add(term, termDictionary.Count);

                TermDocument tempTermDocument;
                tempTermDocument.document = document;
                tempTermDocument.term = term;

                tfDictionary.Add(tempTermDocument, 1);
            }
            else
            {
                TermDocument tempTermDocument;
                tempTermDocument.term = term;
                tempTermDocument.document = document;
                if (tfDictionary.ContainsKey(tempTermDocument))
                {

                    tfDictionary[tempTermDocument]++;
                }
                else
                {
                    tfDictionary.Add(tempTermDocument, 1);
                }
            }
        }


        public Dictionary<TermDocument, int> GetTfDictionary()
        {
            return tfDictionary;
        }

        public int GetNumOfTerms()
        {
            return termDictionary.Count;
        }

        public int GetNumOfDocuments()
        {
            return documentDictionary.Count;
        }


        public void SetTfIdfArray(double[,] tfIdfArray)
        {
            this.tfIdfArray = tfIdfArray;
        }

        public double[,] GetTfIdfArray()
        {
            return tfIdfArray;
        }

        public void SetGfiArray(double[] gfi)
        {
            this.gfi = gfi;
        }


        public void SetGiArray(double[] gi)
        {
            this.gi = gi;
        }


        public int GetTermIdentifier(string term)
        {
            return termDictionary[term];
        }

        public int GetDocumentIdentifier(string document)
        {
            return documentDictionary[document];
        }

        public void setwMatrixReduced(double[,] wMatrixReduced)
        {
            this.wMatrixReduced = wMatrixReduced;
        }

        public double[,] getwMatrixReduced()
        {
            return wMatrixReduced;
        }

        public void setuMatrixReduced(double[,] uMatrixReduced)
        {
            this.uMatrixReduced = uMatrixReduced;
        }

        public double[,] getuMatrixReduced()
        {
            return uMatrixReduced;
        }


        public bool TermExists(string term)
        {
            if (termDictionary.ContainsKey(term))
            {
                return true;
            }
            return false;
        }

    }
}
