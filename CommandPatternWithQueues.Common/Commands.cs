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

        #region Commands
        //command.Metadata.UniqueId
        //command.Metadata.CommandType
        //command.Metadata.IsCorrelated - queue
        //command.Metadata.CorrelationId - queue
        //command.Metadata.OrderId - queue
        //command.Metadata.IsLast - queue
        //command.Metadata.CommandPostedOn
        //command.Metadata.CommandProcessedOn
        //command.Metadata.CommandAttemptedAndFailedOn
        //command.Metadata.CommandRespondedOn
        //command.Metadata.CommandSentToDdlOn

        //ddl.OrginalCommandMetadata
        //ddl.OriginalCommandContext
        //ddl.UniqueId
        //ddl.ErrorMessage
        //ddl.InnerErrorMesssage
        //ddl.PostedToDeadletterQueueOn

        //response.ResponseUniqueId
        //response.ResponseType
        //response.ResponsePostedOn

        #region Single Commands functionality
        public async Task<bool> PostCommand<T>(dynamic CommandContext)
        {
            var commandBody = new ExpandoObject() as dynamic;
            commandBody.Metadata = new ExpandoObject() as dynamic;
            commandBody.CommandContext = new ExpandoObject() as dynamic;

            commandBody.CommandContext = CommandContext;

            commandBody.Metadata.UniqueId = Guid.NewGuid();
            commandBody.Metadata.CommandType = typeof(T).Name;
            commandBody.Metadata.CommandPostedOn = DateTime.UtcNow;


            var r = await qsc_requests.SendMessageAsync(JsonConvert.SerializeObject(commandBody));

            return r != null ? true : false;
        }

        

        public async Task<(bool, int, List<string>)> ExecuteCommands(int timeWindowinMinues = 1)
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
                    var b = await _ProcessCommands(m.MessageText, _log);

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

        #endregion

        #region Queue Commands functionality


        public void AddToQueue<T>(dynamic CommandContext)
        { 
            var commandBody = new ExpandoObject() as dynamic;
            commandBody.Metadata = new ExpandoObject() as dynamic;
            commandBody.CommandContext = new ExpandoObject() as dynamic;

            commandBody.CommandContext = CommandContext;

            queue.Enqueue(commandBody);
        }
       
           
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

       

        private async Task<(bool, Exception, dynamic, dynamic)> _ProcessCommands(string commandBody, ILogger log)
        {
            dynamic context = null;
            dynamic meta = null;
            string type = null;

            try
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(commandBody);
               
                context = (dynamic)m.CommandContext;
                meta = (dynamic)m.Metadata;
                type = meta.CommandType?.ToString();
           
                if (!string.IsNullOrEmpty(type))
                {
                    if (container.IsCommandRegistered(type))
                    {

                        var cmd = container.ResolveCommand(type);

                        var r = await cmd?.ExecuteAsync(context, meta);

                        if (meta != null && r?.Item1) meta.CommandProcessedOn = DateTime.UtcNow;

                        if (cmd.RequiresResponse && r.Item3 != null && r.Item4 != null)
                        {
                            var resp = container.ResolveResponseFromCommand(cmd.GetType());

                            if (resp != null)
                                PostResponse(resp.GetType(), r.Item3, r.Item4);
                        }

                        return r;
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
                if (meta != null) meta.CommandAttemptedAndFailedOn =  DateTime.UtcNow;
                return (false, ex, null, null);
            }
        }
     
        private async Task<bool> _PostCommandToDeadLetter(QueueMessage message, Exception ex)
        {
            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(message.MessageText);
            var context = (dynamic)m.CommandContext;
            var meta = (dynamic)m.Metadata;


            var commandDDLBody = new ExpandoObject() as dynamic;
            commandDDLBody.OrginalCommandMetadata = new ExpandoObject() as dynamic;
            commandDDLBody.OriginalCommandContext = new ExpandoObject() as dynamic;

            commandDDLBody.OrginalCommandMetadata = meta;
            commandDDLBody.OriginalCommandContext = context;

            commandDDLBody.UniqueId =  Guid.NewGuid();
            commandDDLBody.ErrorMessage = ex.Message;
            commandDDLBody.InnerErrorMesssage = ex.InnerException?.Message;
            commandDDLBody.PostedToDeadletterQueueOn = DateTime.UtcNow;

            var r = await qsc_requests_deadletter.SendMessageAsync(JsonConvert.SerializeObject(commandDDLBody));
            
            return r != null;
        }

        #endregion

        #region Responses

        #region Single Response functionality

        public async Task<bool> PostResponse<T>(dynamic ResponseContext, dynamic OriginalCommandMetadata)
        {
            return await PostResponse(typeof(T), ResponseContext, OriginalCommandMetadata);
        }

        public async Task<bool> PostResponse(Type ResponseType, dynamic ResponseContext, dynamic OriginalCommandMetadata)
        {
            var commandBody = new ExpandoObject() as dynamic;
            commandBody.Metadata = OriginalCommandMetadata;
            commandBody.ResponseContext = new ExpandoObject() as dynamic;

            commandBody.ResponseContext = ResponseContext;

            commandBody.Metadata.ResponseUniqueId = Guid.NewGuid();
            commandBody.Metadata.ResponseType = ResponseType.Name;
            commandBody.Metadata.ResponsePostedOn = DateTime.UtcNow;


            var r = await qsc_responses.SendMessageAsync(JsonConvert.SerializeObject(commandBody));

            return r != null ? true : false;
        }

        public async Task<(bool, int, List<string>)> ExecuteResponses(int timeWindowinMinues = 1)
        {
            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {
                QueueMessage[] messages = await qsc_responses.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinues));

                if (messages.Length == 0) break;

                result.Item2 = messages.Length;

                foreach (var m in messages)
                {
                    var b = await _ProcessResponses(m.MessageText, _log);

                    if (b.Item1)
                    {
                        _ = await qsc_responses.DeleteMessageAsync(m.MessageId, m.PopReceipt);
                    }
                    else
                    {
                        result.Item1 = false;
                        result.Item3.Add($"{m.MessageId}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

                        _log.LogError(b.Item2?.Message);
                        _log.LogWarning($"Message could not be processed: [{qsc_responses.Name}] {m.MessageText}");

                        if (m.DequeueCount >= _MAX_DEQUEUE_COUNT_FOR_ERROR)
                        {
                            _log.LogWarning($"Message {m.MessageId} will be moved to dead letter queue.");

                            _ = await _PostResponseToDeadLetter(m, b.Item2);
                            _ = await qsc_responses.DeleteMessageAsync(m.MessageId, m.PopReceipt);
                        }
                    }
                }

                QueueProperties properties = await qsc_responses.GetPropertiesAsync();
                _log.LogWarning($"[{qsc_responses.Name}] {properties.ApproximateMessagesCount} messages left in queue.");
            }

            return result;
        }



        #endregion


        private async Task<(bool, Exception)> _ProcessResponses(string responseBody, ILogger log)
        {
            dynamic context = null;
            dynamic meta = null;
            string type = null;

            try
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(responseBody);

                context = (dynamic)m.ResponseContext;
                meta = (dynamic)m.Metadata;
                type = meta.ResponseType?.ToString();

                if (!string.IsNullOrEmpty(type))
                {

                    if (container.IsResponseRegistered(type))
                    {
                        var r = await container.ResolveResponse(type)?.ExecuteAsync(context, meta);

                        if (meta != null && r.Item1) meta.ResponseProcessedOn = DateTime.UtcNow;

                        return r;

                       
                    }
                    else
                    {
                        log.LogError($"Response type [{type}] is not registered with the container and cannot be processed. Skipping.");
                        throw new ApplicationException($"Response type [{type}] is not registered with the container and cannot be processed.");
                    }
                }
                else
                {
                    throw new ApplicationException("Response type was null or empty.");
                }
            }
            catch (Exception ex)
            {
                if (meta != null) meta.ResponseAttemptedAndFailedOn = DateTime.UtcNow;
                return (false, ex);
            }
        }

        private async Task<bool> _PostResponseToDeadLetter(QueueMessage message, Exception ex)
        {
            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(message.MessageText);
            var context = (dynamic)m.CommandContext;
            var meta = (dynamic)m.Metadata;


            var responseDDLBody = new ExpandoObject() as dynamic;
            responseDDLBody.OrginalResponseMetadata = new ExpandoObject() as dynamic;
            responseDDLBody.OriginalResponseContext = new ExpandoObject() as dynamic;

            responseDDLBody.OrginalResponseMetadata = meta;
            responseDDLBody.OriginalResponseContext = context;

            responseDDLBody.UniqueId =  Guid.NewGuid();
            responseDDLBody.ErrorMessage = ex.Message;
            responseDDLBody.InnerErrorMesssage = ex.InnerException?.Message;
            responseDDLBody.PostedToDeadletterQueueOn = DateTime.UtcNow;

            var r = await qsc_responses_deadletter.SendMessageAsync(JsonConvert.SerializeObject(responseDDLBody));
            
            return r != null;
        }

        #endregion

        #region Helper functions


        public static bool DoesPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);

            return obj.GetType().GetProperty(name) != null;
        }




        #endregion
    }
}
