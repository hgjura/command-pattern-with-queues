using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public abstract class GenericCommandBase : CommandBase
    {
        public abstract Task<(bool, Exception)> ExecuteAsync(dynamic command, dynamic metadata, ILogger logger = null);
    }
}
