using Microsoft.ServiceBus.Messaging;
using MyWebRole;
using MyWebRole.Connectors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextPreprocessorWorkerRole.Checkers
{
    struct TokenResult
    {
        public string token;
        public bool result;
    }

    class SpellChecker
    {

        private string[] tokens;
        public List<string> wrongSpelledWordsList { get; set; }
        private List<TokenResult> tokenList;
        public SpellChecker(string[] tokens)
        {
            this.tokens = tokens;
            wrongSpelledWordsList = new List<string>();
            tokenList = new List<TokenResult>();
            foreach (string token in tokens)
            {
                TokenResult tokenResult = new TokenResult();
                tokenResult.token = token;
                tokenResult.result = true;
                tokenList.Add(tokenResult);
            }
        }


        public void checkSpelling()
        {
            
            string id = DateTime.UtcNow.ToFileTimeUtc().ToString();
            string jsonTokenList = JsonConvert.SerializeObject(tokenList);
            int size = Encoding.UTF8.GetByteCount(jsonTokenList);
            BrokeredMessage brokeredMessage = new BrokeredMessage();
            brokeredMessage.Properties["id"] = id;
            brokeredMessage.Properties["token"] = jsonTokenList;
            WorkerRole.queueSendTokenConnector.SendMessage(brokeredMessage);

            Trace.WriteLine("Spell Checking message with id " + id + " at the file " + WorkerRole.fileName + " is sent");

            bool receivedFlag = false;
            while (!receivedFlag)
            {
                // Receive the message
                BrokeredMessage receivedMessage = WorkerRole.queueReceiveTokenConnector.ReceiveMessage();
                Trace.WriteLine("Waiting for message with id " + id);

                if (receivedMessage != null)
                {
                    Trace.WriteLine("Something received");
                    string receivedId = receivedMessage.Properties["id"].ToString();
                    if (receivedId.Equals(id))
                    {
                        Trace.WriteLine("Spell Checking message with id " + id + " at the file " + WorkerRole.fileName + " received");

                        string resultJson = receivedMessage.Properties["result"].ToString();
                        receivedMessage.Complete();
                        receivedMessage = null;
                        List<TokenResult> resultList = (List<TokenResult>)JsonConvert.DeserializeObject<List<TokenResult>>(resultJson);
                        receivedFlag = true;
                        foreach (TokenResult tempToken in resultList)
                        {
                            if (!tempToken.result)
                            {
                                wrongSpelledWordsList.Add(tempToken.token);
                            }
                        }
                        //if (!result)
                        //{
                        //    wrongSpelledWordsList.Add(token);
                        //}
                    }
                    else
                    {
                        Trace.WriteLine("Spell Checking message with id " + id + " at the file " + WorkerRole.fileName + " not received. Abandonded");
                        receivedMessage.Abandon();
                    }
                }
                else
                {
                    Trace.WriteLine("Spell Checking message with id " + id + " at the file " + WorkerRole.fileName + " still waiting");
                }
            }
            //foreach (string token in tokens)
            //{
            //    string id = DateTime.UtcNow.ToFileTimeUtc().ToString();

            //    queueSendTokenConnector.SendMessage(id, token);

            //    bool receivedFlag = false;
            //    while (!receivedFlag)
            //    {
            //        // Receive the message
            //        BrokeredMessage receivedMessage = queueReceiveTokenConnector.ReceiveMessage();

            //        if (receivedMessage != null)
            //        {
            //            string receivedId = receivedMessage.Properties["id"].ToString();
            //            if (receivedId.Equals(id))
            //            {
            //                bool result = (bool)receivedMessage.Properties["result"];
            //                receivedMessage.Complete();
            //                receivedFlag = true;
            //                if (!result)
            //                {
            //                    wrongSpelledWordsList.Add(token);
            //                }
            //            }
            //            else
            //            {
            //                receivedMessage.Abandon();
            //            }
            //        }
            //    }
            //}

        }
    }
}
