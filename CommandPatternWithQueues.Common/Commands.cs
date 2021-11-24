using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public class Commands
    {
        readonly string _CONNECTIONSTRING = null;
        readonly long _MAX_DEQUEUE_COUNT_FOR_ERROR = 5;
        
        static ILogger _log;
        static HttpClient _client;
        Policy _policy;
        static QueueClient queueClient;
        static QueueClient queueClient_deadletter;


        public Commands(string StorageConnectionString, HttpClient Client, ILogger Log, Policy RetryPolicy = null, string QueueName = null)
        {
           
            queueClient = new QueueClient(StorageConnectionString, string.IsNullOrEmpty(QueueName) ? "command-requests" : QueueName);
            queueClient.CreateIfNotExists();

            _CONNECTIONSTRING ??= StorageConnectionString;
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

            var r = await queueClient.SendMessageAsync(JsonConvert.SerializeObject(command));

            return r != null ? true : false;
        }
        
        public async Task<(bool, int, List<string>)> GetCommands(int timeWindowinMinues = 1)
        {
            var result = (true, 0, new List<string>());

            while (true)
            {
                QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinues));

                if (messages.Length == 0) break;

                result.Item2 = messages.Length;

                foreach (var m in messages)
                {
                    var b = await _ProcessCommands(m.MessageText, _log, _client);

                    if (b.Item1)
                    {
                        _ = await queueClient.DeleteMessageAsync(m.MessageId, m.PopReceipt);
                    }
                    else
                    {
                        result.Item1 = false;
                        result.Item3.Add($"{m.MessageId}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

                        _log.LogError(b.Item2?.Message);
                        _log.LogWarning($"Message could not be processed: [{queueClient.Name}] {m.MessageText}");
                        
                        if (m.DequeueCount >= _MAX_DEQUEUE_COUNT_FOR_ERROR)
                        {
                            _log.LogWarning($"Message {m.MessageId} will be moved to dead letter queue.");

                           _ = await _PostCommandToDeadLetter(m, b.Item2);
                           _ = await queueClient.DeleteMessageAsync(m.MessageId, m.PopReceipt);
                        }
                    }
                }

                QueueProperties properties = await queueClient.GetPropertiesAsync();
                _log.LogWarning($"[{queueClient.Name}] {properties.ApproximateMessagesCount} messages left in queue.");
            }

            return result;
        }


        #region Commands Stack functionality

        private Queue<dynamic> queue = new Queue<dynamic>();
        
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

                var r = await queueClient.SendMessageAsync(JsonConvert.SerializeObject(command));
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
                    dynamic t = Assembly.GetExecutingAssembly().GetTypes().First(t => t.Name == type);

                    var i = Activator.CreateInstance(t);

                    if (i is GenericCommandBase)
                    {
                        return await t.InvokeMember("ExecuteAsync", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, i, new object[] { context, meta, log });
                    }
                    else if (i is WebCommandBase)
                    {
                        return await t.InvokeMember("ExecuteAsync", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, i, new object[] { context, meta, log, client });
                    }
                    else
                    {
                        throw new ApplicationException($"Unknown command base type {t}");
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
            var c = queueClient_deadletter ?? _CreateDeadLetterQueue();

            dynamic commandBody = new ExpandoObject();
            commandBody.ErrorMesssage = ex.Message;
            commandBody.InnerErrorMesssage = ex.InnerException?.Message;
            commandBody.OriginalMessageId = Guid.NewGuid();
            commandBody.OriginalMessageBody = message.MessageText;

            var r = await c.SendMessageAsync(JsonConvert.SerializeObject(commandBody));
            return true;
        }

        private QueueClient _CreateDeadLetterQueue()
        {
            queueClient_deadletter = new QueueClient(_CONNECTIONSTRING, "commands-deadletterqueue");
            queueClient_deadletter.CreateIfNotExists();
            return queueClient_deadletter;
        }

    }
}
