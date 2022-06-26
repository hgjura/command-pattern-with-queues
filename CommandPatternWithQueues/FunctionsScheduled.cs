using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CommandPatternWithQueues.ExecutingFunctions
{
    public class FunctionsScheduled
    {
        [FunctionName("HandleDeadletterqueue")]
        public async Task RunAsync([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer, ILogger log)
        {
            //this runs every hour, on the hour
            log.LogInformation($"Starting deadletter queue handling: {DateTime.Now}");

            await FunctionImplementations.FunctionWrapperHandleDlqAsync(FunctionSettingsEternalDurable.Configuration, log);
        }
    }
}
