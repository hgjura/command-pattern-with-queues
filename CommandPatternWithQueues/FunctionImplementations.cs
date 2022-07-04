using CommandPatternWithQueues.RemoteCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerTools.ServerCommands;
using ServerTools.ServerCommands.AzureStorageQueues;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.ExecutingFunctions
{
    public class FunctionImplementations
    {
        public static async Task<string> PostCommandsAsync(IConfiguration config, ILogger logger)
        {

            try
            {

                var c = await new CloudCommands().InitializeAsync(new CommandContainer(), new AzureStorageQueuesConnectionOptions(config["StorageAccountName"], config["StorageAccountKey"], 3, logger, QueueNamePrefix: "test-project"));

                _ = await c.PostCommandAsync<RandomCatCommand>(new { Name = "Laika" });

                _ = await c.PostCommandAsync<RandomDogCommand>(new { Name = "Scooby-Doo" });

                _ = await c.PostCommandAsync<RandomFoxCommand>(new { Name = "Penny" });

                _ = await c.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });


                return "Ok.";
            }
            catch (Exception ex)
            {
                return ex.Message;

            }

        }

        public static async Task<int> FunctionWrapperExecuteCommandsAsync(IConfiguration config, ILogger logger)
        {
            int r = 0;

            try
            {
                r = await ExecuteCommandsAsync(config, logger);

                if (r > 0)
                {
                    logger.LogWarning($"{r} commands were succesfuly executed.");
                }
                else
                {
                    logger.LogWarning($"No commands were executed. Pausing for {FunctionSettingsEternalDurable.MinutesToWaitAfterNoCommandsExecuted} min(s).");

                    r = FunctionSettingsEternalDurable.MinutesToWaitAfterNoCommandsExecuted;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} [{ex.InnerException?.Message}]");
                logger.LogWarning($"An error ocurred. Pausing for {FunctionSettingsEternalDurable.MinutesToWaitAfterErrorInCommandsExecution} min(s).");

                r = FunctionSettingsEternalDurable.MinutesToWaitAfterErrorInCommandsExecution;
            }

            return r;
        }
        private static async Task<int> ExecuteCommandsAsync(IConfiguration config, ILogger logger)
        {
            var _container = new CommandContainer();

            using var client = new HttpClient();

            _container
                .Use(logger)
                .Use(client)
                .RegisterCommand<RandomCatCommand>()
                .RegisterCommand<RandomDogCommand>()
                .RegisterCommand<RandomFoxCommand>()
                .RegisterCommand<AddNumbersCommand, AddNumbersResponse>();

            var c = await new CloudCommands().InitializeAsync(_container, new AzureStorageQueuesConnectionOptions(config["StorageAccountName"], config["StorageAccountKey"], 3, logger, QueueNamePrefix: "test-project"));

            var result1 = await c.ExecuteCommandsAsync();

            var result2 = await c.ExecuteResponsesAsync();

            // return number of commands + responses executed
            return result1.Item2 + result2.Item2;
        }

        public static async Task<string> FunctionWrapperHandleDlqAsync(IConfiguration config, ILogger logger)
        {
            try
            {
                return await HandleDlqAsync(config, logger);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }      
        private static async Task<string> HandleDlqAsync(IConfiguration config, ILogger logger)
        {
            var commands = await new CloudCommands().InitializeAsync(new CommandContainer(), new AzureStorageQueuesConnectionOptions(config["StorageAccountName"], config["StorageAccountKey"], 3, logger, QueueNamePrefix: "test-project"));

            var c = await commands.HandleCommandsDlqAsync();
            var r = await commands.HandleResponsesDlqAsync();

            return c.Item1 && r.Item1 ? "Ok." : "Some errors occorred while processing the dlq.";
        }
    }
}
