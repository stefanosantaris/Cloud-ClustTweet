using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextPreprocessorWorkerRole.Checkers
{
    class SlangChecker
    {
        public string[] tokens { get; set; }
        public List<string> wrongSpelledTokens { get; set; }
        public int numOfSlangs { get; set; }


        public SlangChecker()
        {
            numOfSlangs = 0;
        }

        public void checkForSlangs(string[] tokens, List<string> wrongSpelledTokens)
        {
            this.tokens = tokens;
            //DBConnect connect = new DBConnect();
            foreach (string slangToken in wrongSpelledTokens)
            {
                if (WorkerRole.slangDictionary.ContainsKey(slangToken))
                {
                    string translation = WorkerRole.slangDictionary[slangToken];
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (tokens[i].Equals(slangToken))
                        {
                            this.tokens[i] = translation;
                            numOfSlangs++;
                        }
                    }
                }
            }
        }
    }
}
