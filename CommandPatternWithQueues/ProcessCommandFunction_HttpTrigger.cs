using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using CommandPatternWithQueues.Common;

namespace CommandPatternWithQueues.Communication
{
    public class ProcessCommandFunction_HttpTrigger
    {
        static IHttpClientFactory factory;
        public ProcessCommandFunction_HttpTrigger(IHttpClientFactory httpClientFactory)
        {
            factory = httpClientFactory;
        }

        [Disable("HTTP_TRIGGER_DISABLED")]
        [FunctionName("ProcessCommandFunction_HttpTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {

                string _STORAGE_CONNECTIONSTRING = Environment.GetEnvironmentVariable("StorageConnectionString");
                var c = new Commands(_STORAGE_CONNECTIONSTRING, factory.CreateClient(), log);

                _ = await c.PostCommand<RandomCatCommand>(new { Cat = "Laika" });

                _ = await c.PostCommand<RandomDogCommand>(new { Dog = "Scooby-Doo" });

                _ = await c.PostCommand<AddNumbersCommand>(new { Number1 = 2, Number3 = 3 });

                var result = await c.ExecuteCommands();

                if (result.Item1)
                {
                    return result.Item2 > 0 ? new OkObjectResult("Ok.") : new OkObjectResult("No items were processed.");
                }
                else
                    return new BadRequestObjectResult($"Items were processed with errors:\n {string.Join("\n", result.Item3) }");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
            
        }
    }
}
