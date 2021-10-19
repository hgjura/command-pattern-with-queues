using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommandPatternWithQueues.Common
{
    public abstract class CommandBase
    {
        internal static ILogger logger;
        internal static QueueClient queueClient;
        public CommandBase()
        {
            queueClient = new QueueClient("DefaultEndpointsProtocol=https;AccountName=cbsaauditlyf2e5k26ogpjw;AccountKey=XiKXwt+xShQv4t5nH0CI7IN2bBq9wvQP8Px6maO91M2B7SkKKlaeSpROw/A9hSgsSObp74Jokdw1kVKM21I4Xg==;EndpointSuffix=core.windows.net", "commands");
            queueClient.CreateIfNotExists();
        }

    }
}
