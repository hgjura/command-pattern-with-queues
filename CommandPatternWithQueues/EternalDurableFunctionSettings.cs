using CommandPatternWithQueues.RemoteCommands;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using ServerTools.ServerCommands;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.ExecutingFunctions
{
    public class EternalDurableFunctionSettings
    {
        internal const string FunctionId = "server_commands_func";

        internal const string FunctionOrchestratorName = $"{FunctionId}_orchestrator";
        internal const string FunctionHttpStartName = $"{FunctionId}_httpstart";
        internal const string FunctionExecutorName = $"{FunctionId}_executor";
        internal const string FunctionHttpTrigger = $"{FunctionId}_httptrigger";

        internal const int MinutesToWaitAfterNoCommandsExecuted = 1;
        internal const int MinutesToWaitAfterErrorInCommandsExecution = 3;

        static IConfiguration Configuration { get; set; }

        public static void FunctionStartupConfigure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient<IHttpClientFactory>()
               .SetHandlerLifetime(TimeSpan.FromMinutes(15))
               .AddPolicyHandler(HttpPolicyExtensions
                  .HandleTransientHttpError()
                  .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                  .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                  );
        }

        public static void FunctionConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "local.settings.json"), optional: true, reloadOnChange: false)
                .AddUserSecrets<EternalDurableFunctionSettings>(true)
                .AddEnvironmentVariables();

            Configuration = builder.ConfigurationBuilder.Build();
        }




        public static async Task<int> FunctionExecuteAsync(ILogger logger)
        {
            int r = 0;

            try
            {
                r = await ExecuteCommandsAsync(logger);

                if (r > 0)
                {
                    logger.LogWarning($"{r} commands were succesfuly executed.");
                } 
                else
                {
                    logger.LogWarning($"No commands were executed. Pausing for {MinutesToWaitAfterNoCommandsExecuted} min(s).");

                    r = MinutesToWaitAfterNoCommandsExecuted;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} [{ex.InnerException?.Message}]");
                logger.LogWarning($"An error ocurred. Pausing for {MinutesToWaitAfterErrorInCommandsExecution} min(s).");

                r = MinutesToWaitAfterErrorInCommandsExecution;
            }

            return r;            
        }

        private static async Task<int> ExecuteCommandsAsync(ILogger logger)
        {
            var _container = new CommandContainer();

            using var client = new HttpClient();

            _container
                .Use(logger)
                .Use(client)
                .RegisterCommand<RandomCatCommand>()
                .RegisterCommand<RandomDogCommand>()
                .RegisterCommand<RandomFoxCommand>()
                .RegisterCommand<AddNumbersCommand>()
                .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();

            var c = new Commands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], logger);


            var result1 = await c.ExecuteCommands();

            var result2 = await c.ExecuteResponses();

            // return number of commands + responses executed
            return result1.Item2 + result2.Item2;
        }


        public static async Task<string> FunctionHttpTriggerExecuteAsync(ILogger logger)
        {

            try
            {
                var _container = new CommandContainer();

                _container
                    .RegisterCommand<RandomCatCommand>()
                    .RegisterCommand<RandomDogCommand>()
                    .RegisterCommand<RandomFoxCommand>()
                    .RegisterCommand<AddNumbersCommand>()
                    .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();

                var c = new Commands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], null);

                _ = await c.PostCommand<RandomCatCommand>(new { Name = "Laika" });

                _ = await c.PostCommand<RandomDogCommand>(new { Name = "Scooby-Doo" });

                _ = await c.PostCommand<RandomFoxCommand>(new { Name = "Penny" });

                _ = await c.PostCommand<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });


                return "Ok.";
            }
            catch (Exception ex)
            {
                return ex.Message;
          
            }
            
        }
    }
}
