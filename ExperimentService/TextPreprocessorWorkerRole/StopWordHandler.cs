using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TextPreprocessorWorkerRole
{
    class StopWordHandler
    {
        private HashSet<string> stopHashSet;
        public StopWordHandler()
        {
            stopHashSet = new HashSet<string>();
            initializeStopList();
        }

        private void initializeStopList()
        {
            TextReader reader = new StreamReader("StopList.txt");
            string sText = reader.ReadToEnd();
            reader.Close();

            Regex oRegex = new Regex("([ \\t{}():;. \n])");
            sText = sText.ToLower();

            String[] words = oRegex.Split(sText);
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = words[i].Trim();
            }

            for (int i = 0; i < words.Length; i++)
            {
                MatchCollection mc = oRegex.Matches(words[i]);
                if (mc.Count <= 0 && words[i].Trim().Length > 0 && !stopHashSet.Contains(words[i]))
                {
                    stopHashSet.Add(words[i]);
                }
            }

        }


        public bool isStopWord(string word)
        {
            return stopHashSet.Contains(word);
        }



    }
}
