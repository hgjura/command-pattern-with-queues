using Azure.Storage;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public class Commands
    {
        readonly CommandContainer container;
        readonly long _MAX_DEQUEUE_COUNT_FOR_ERROR = 5;

        readonly ILogger _log;
        readonly HttpClient _client;

        readonly Queue<dynamic> queue = new Queue<dynamic>();

        readonly QueueClient qsc_requests;
        readonly QueueClient qsc_requests_deadletter;
        readonly QueueClient qsc_responses;
        readonly QueueClient qsc_responses_deadletter;
        
        Policy _policy;

        public Commands(CommandContainer Container, string AccountName, string AccountKey, HttpClient Client, ILogger Log, Policy RetryPolicy = null, string QueueNamePrefix = null)
        {
            container = Container;

            qsc_requests ??= new QueueClient(new Uri($"https://{AccountName}.queue.core.windows.net/{(string.IsNullOrEmpty(QueueNamePrefix) ? "command-requests" : $"{QueueNamePrefix}-requests")}"), new StorageSharedKeyCredential(AccountName, AccountKey));
            qsc_requests_deadletter ??= new QueueClient(new Uri($"https://{AccountName}.queue.core.windows.net/{(string.IsNullOrEmpty(QueueNamePrefix) ? "command-requests-deadletter" : $"{QueueNamePrefix}-requests-deadletter")}"), new StorageSharedKeyCredential(AccountName, AccountKey));
            qsc_responses ??= new QueueClient(new Uri($"https://{AccountName}.queue.core.windows.net/{(string.IsNullOrEmpty(QueueNamePrefix) ? "command-responses" : $"{QueueNamePrefix}-responses")}"), new StorageSharedKeyCredential(AccountName, AccountKey));
            qsc_responses_deadletter ??= new QueueClient(new Uri($"https://{AccountName}.queue.core.windows.net/{(string.IsNullOrEmpty(QueueNamePrefix) ? "command-responses-deadletter" : $"{QueueNamePrefix}-responses-deadletter")}"), new StorageSharedKeyCredential(AccountName, AccountKey));

            qsc_requests.CreateIfNotExists();
            qsc_requests_deadletter.CreateIfNotExists();
            qsc_responses.CreateIfNotExists();
            qsc_responses_deadletter.CreateIfNotExists();

            _client ??= Client;
            _log ??= Log;

            this._policy = RetryPolicy ?? Policy
               .Handle<Exception>()
               .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (result, timeSpan, retryCount, context) =>
               {
                   Log.LogWarning($"Calling service failed [{result.Message} | {result.InnerException?.Message}]. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
               });

        }

        public async Task<bool> PostCommand<T>(dynamic CommandContext)
        {
           
            var command = _PrepareCommandBody(CommandContext, typeof(T).Name);

            command.Metadata.PostedOn = DateTime.UtcNow;

            var r = await qsc_requests.SendMessageAsync(JsonConvert.SerializeObject(command));

            return r != null ? true : false;
        }
        
        public async Task<(bool, int, List<string>)> GetCommands(int timeWindowinMinues = 1)
        {
            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {
                QueueMessage[] messages = await qsc_requests.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinues));

                if (messages.Length == 0) break;

                result.Item2 = messages.Length;

                foreach (var m in messages)
                {
                    var b = await _ProcessCommands(m.MessageText, _log, _client);

                    if (b.Item1)
                    {
                        _ = await qsc_requests.DeleteMessageAsync(m.MessageId, m.PopReceipt);
                    }
                    else
                    {
                        result.Item1 = false;
                        result.Item3.Add($"{m.MessageId}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

                        _log.LogError(b.Item2?.Message);
                        _log.LogWarning($"Message could not be processed: [{qsc_requests.Name}] {m.MessageText}");
                        
                        if (m.DequeueCount >= _MAX_DEQUEUE_COUNT_FOR_ERROR)
                        {
                            _log.LogWarning($"Message {m.MessageId} will be moved to dead letter queue.");

                           _ = await _PostCommandToDeadLetter(m, b.Item2);
                           _ = await qsc_requests.DeleteMessageAsync(m.MessageId, m.PopReceipt);
                        }
                    }
                }

                QueueProperties properties = await qsc_requests.GetPropertiesAsync();
                _log.LogWarning($"[{qsc_requests.Name}] {properties.ApproximateMessagesCount} messages left in queue.");
            }

            return result;
        }


        #region Commands Stack functionality

        
        public void AddToQueue<T>(dynamic CommandContext) => queue.Enqueue(_PrepareCommandBody(CommandContext, typeof(T).Name));
           
        public async Task FlushQueueAsync()
        {
            Guid correlationId = Guid.NewGuid();
            int queueorder = 0;
            int islast = queue.Count;

            while (queue.Count > 0)
            {
                queueorder++;

                var command = queue.Dequeue();

                command.Metadata.IsCorrelated = true;
                command.Metadata.CorrelationId = correlationId;
                command.Metadata.OrderId = queueorder;
                command.Metadata.IsLast = queue.Count == 0;

                command.Metadata.PostedOn = DateTime.UtcNow;

                var r = await qsc_requests.SendMessageAsync(JsonConvert.SerializeObject(command));
            }
        }


        #endregion

        public static bool DoesPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);

            return obj.GetType().GetProperty(name) != null;
        }

        private dynamic _PrepareCommandBody(dynamic commandcontext, string commandname)
        {
            dynamic commandBody = new ExpandoObject();
            
            commandBody.CommandContext = commandcontext;
            
            commandBody.Metadata = new ExpandoObject() as dynamic;

            commandBody.Metadata.UniqueId = Guid.NewGuid();
            commandBody.Metadata.CommandType = commandname;

            return commandBody;
        }

        private async Task<(bool, Exception)> _ProcessCommands(string commandBody, ILogger log = null, HttpClient client = null)
        {
            try
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(commandBody);
               
                var context = (dynamic)m.CommandContext;
                var meta = (dynamic)m.Metadata;
                var type = meta.CommandType?.ToString();
           
                if (!string.IsNullOrEmpty(type))
                {

                    if (container.IsRegistered(type))
                    {
                        return await container.Resolve(type)?.ExecuteAsync(context, meta);
                    }
                    else 
                    {
                        log.LogError($"Command type [{type}] is not registered with the container and cannot be processed. Skipping.");
                        throw new ApplicationException($"Command type [{type}] is not registered with the container and cannot be processed.");
                    }                        
                }
                else
                {
                    throw new ApplicationException("Command type was null or empty.");
                }
            }
            catch (Exception ex)
            { 
                return (false, ex);
            }
        }
     
        private async Task<bool> _PostCommandToDeadLetter(QueueMessage message, Exception ex)
        {
            dynamic commandBody = new ExpandoObject();
            commandBody.ErrorMesssage = ex.Message;
            commandBody.InnerErrorMesssage = ex.InnerException?.Message;
            commandBody.OriginalMessageId = Guid.NewGuid();
            commandBody.OriginalMessageBody = message.MessageText;

            var r = await qsc_requests_deadletter.SendMessageAsync(JsonConvert.SerializeObject(commandBody));
            return true;
        }

    }
}
