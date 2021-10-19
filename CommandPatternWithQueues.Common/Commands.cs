using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
        CommandMapper map;
        static QueueClient queueClient;
        static QueueClient queueClient_deadletter;


        public Commands(string StorageConnectionString, HttpClient Client, ILogger Log, Policy RetryPolicy = null)
        {
            map = new CommandMapper();

            queueClient = new QueueClient(StorageConnectionString, "commands");
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

            dynamic commandBody = new ExpandoObject();
            commandBody.CommandType = typeof(T).Name;
            commandBody.UniqueId = Guid.NewGuid();
            commandBody.CommandContext = CommandContext;

            var r = await queueClient.SendMessageAsync(JsonConvert.SerializeObject(commandBody));

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

        private async Task<(bool, Exception)> _ProcessCommands(string commandBody, ILogger log = null, HttpClient client = null)
        {
            try
            {

                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(commandBody);
                var type = m.CommandType?.ToString();
                var context = (dynamic)m.CommandContext;
                var id = Guid.Parse(m.UniqueId);

                if (!string.IsNullOrEmpty(type))
                {
                    var t = map.Get(type);

                    var i = Activator.CreateInstance(t);


                    if (i is GenericCommandBase)
                    {
                        return await t.InvokeMember("ExecuteAsync", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, i, new object[] { context, log });
                    }
                    else if (i is WebCommandBase)
                    {
                        return await t.InvokeMember("ExecuteAsync", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, i, new object[] { context, log, client });
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
