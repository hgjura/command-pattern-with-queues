using CommandPatternWithQueues.RemoteCommands;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using ServerTools.ServerCommands;
using ServerTools.ServerCommands.AzureStorageQueues;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.ExecutingFunctions
{
    public class FunctionSettingsEternalDurable
    {
        internal const string FunctionId = "server_commands_func";

        internal const string FunctionOrchestratorName = $"{FunctionId}_orchestrator";
        internal const string FunctionHttpStartName = $"{FunctionId}_httpstart";
        internal const string FunctionExecutorName = $"{FunctionId}_executor";
        internal const string FunctionHttpTrigger = $"{FunctionId}_httptrigger";

        internal const int MinutesToWaitAfterNoCommandsExecuted = 1;
        internal const int MinutesToWaitAfterErrorInCommandsExecution = 3;

        public static IConfiguration Configuration { get; set; }

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
                .AddUserSecrets<FunctionSettingsEternalDurable>(true)
                .AddEnvironmentVariables();

            Configuration = builder.ConfigurationBuilder.Build();

        }

        public static async Task<int> FunctionExecuteAsync(ILogger logger)
        {
            int r = 0;

            try
            {
                r = await FunctionImplementations.FunctionWrapperExecuteCommandsAsync(Configuration, logger);

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
    }
}
