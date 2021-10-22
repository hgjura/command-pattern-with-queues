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
            queueClient = new QueueClient(Environment.GetEnvironmentVariable("StorageConnectionString"), "commands");
            queueClient.CreateIfNotExists();
        }

    }
}
