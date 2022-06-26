using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommandPatternWithQueues.ExecutingFunctions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CommandPatternWithQueues.ExecutingFunctions
{
    public class FunctionsDurable
    {
        [FunctionName(FunctionSettingsEternalDurable.FunctionHttpStartName)]
        public async Task<HttpResponseMessage> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            var existingInstance = await starter.GetStatusAsync(FunctionSettingsEternalDurable.FunctionId);
            if (
                existingInstance == null
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
            {
                string instanceId = await starter.StartNewAsync(FunctionSettingsEternalDurable.FunctionOrchestratorName, FunctionSettingsEternalDurable.FunctionId);

                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

                return starter.CreateCheckStatusResponse(req, FunctionSettingsEternalDurable.FunctionId);
            }
            else
            {
                // An instance with the specified ID exists or an existing one still running, don't create one.
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent($"An instance with ID '{FunctionSettingsEternalDurable.FunctionId}' already exists."),
                };
            }
        }


        [FunctionName(FunctionSettingsEternalDurable.FunctionOrchestratorName)]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var r = await context.CallActivityAsync<int>(FunctionSettingsEternalDurable.FunctionExecutorName, null);

            if(r > 0)
                await context.CreateTimer(context.CurrentUtcDateTime.Add(TimeSpan.FromMinutes(r)), CancellationToken.None);

            context.ContinueAsNew(null);
        }

        [FunctionName(FunctionSettingsEternalDurable.FunctionExecutorName)]
        public async Task<int> ExecuteCommandsAsync([ActivityTrigger] object input, ILogger log)
        {
            return await FunctionSettingsEternalDurable.FunctionExecuteAsync(log);
        }

        [FunctionName(FunctionSettingsEternalDurable.FunctionHttpTrigger)]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            return new OkObjectResult(await FunctionImplementations.PostCommandsAsync(FunctionSettingsEternalDurable.Configuration, log));
        }
    }

}










