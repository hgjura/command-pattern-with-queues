using CommandPatternWithQueues.RemoteCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerTools.ServerCommands;
using ServerTools.ServerCommands.AzureStorageQueues;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Tests
{
    [TestClass]
    public class TestingRemoteCommands
    {

        static CommandContainer _container;
        static string _queueNamePrefix;


        static IConfiguration Configuration { get; set; }


        /// <summary>
        /// Execute once before the test-suite
        /// </summary>
        /// 

        [ClassInitialize()]
        public static async Task InitTestSuiteAsync(TestContext testContext)
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<TestingRemoteCommands>(true)
                .AddJsonFile("local.tests.settings.json", true);

            Configuration = builder.Build();

            _container = new CommandContainer();
            _queueNamePrefix = nameof(TestingRemoteCommands).ToLower();

            _ = await new CloudCommands().InitializeAsync(_container, new AzureStorageQueuesConnectionOptions(Configuration["StorageAccountName"], Configuration["StorageAccountKey"], 3, null, QueueNamePrefix: _queueNamePrefix));

        }

        [ClassCleanup()]
        public static async Task CleanTestSuiteAsync()
        {
            _ = (await new CloudCommands().InitializeAsync(_container, new AzureStorageQueuesConnectionOptions(Configuration["StorageAccountName"], Configuration["StorageAccountKey"], 3, null, QueueNamePrefix: _queueNamePrefix))).ClearAllAsync();
        }



        [TestMethod]
        public async Task A1000_TestingSimplePostAndRetriveCommandsAsync()
        {

            var logger = new DebugLoggerProvider().CreateLogger("default");
            using var client = new HttpClient();

            _container
                .Use(logger)
                .Use(client)
                .RegisterCommand<RandomCatCommand>()
                .RegisterCommand<RandomDogCommand>()
                .RegisterCommand<RandomFoxCommand>()
                .RegisterCommand<AddNumbersCommand>()
                .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();

            var c = await new CloudCommands().InitializeAsync(_container, new AzureStorageQueuesConnectionOptions(Configuration["StorageAccountName"], Configuration["StorageAccountKey"], 3, logger, QueueNamePrefix: _queueNamePrefix));


            _ = await c.PostCommandAsync<RandomCatCommand>(new { Name = "Laika" });

            _ = await c.PostCommandAsync<RandomDogCommand>(new { Name = "Scooby-Doo" });

            _ = await c.PostCommandAsync<RandomFoxCommand>(new { Name = "Penny" });

            _ = await c.PostCommandAsync<AddNumbersCommand > (new { Number1 = 2, Number2 = 3 });

            var result1 = await c.ExecuteCommandsAsync();

            var result2 = await c.ExecuteResponsesAsync();

            //check if something was wrong or if any items were processed at all
            Assert.IsTrue(result1.Item1); //This value will return true if any items were processed AND there was no errors

            //check if 1 or more items were processed
            Assert.IsTrue(result1.Item2 > 0 && result1.Item2 == 4); //This value keeps the amount of commands that were processed 

            //check if there was any errors
            Assert.IsTrue(result1.Item3.Count == 0); //This value keeps the list of error messages that were encountered. The format of the messages is "{MessageId}:{Exception Messgae}". After retrying x amount of times the message is moved to the deadletterqueue (see code for how this is implemented)


            //check if something was wrong or if any items were processed at all
            Assert.IsTrue(result2.Item1); //This value will return true if any items were processed AND there was no errors

            //check if 1 or more items were processed
            Assert.IsTrue(result2.Item2 > 0 && result2.Item2 == 1); //This value keeps the amount of commands that were processed 

            //check if there was any errors
            Assert.IsTrue(result2.Item3.Count == 0);
        }

        [TestMethod]
        public async Task TestingAttemptToProcessCommandsThatAreNotRegisteredWithContainer()
        {

            var logger = new DebugLoggerProvider().CreateLogger("default");
            using var client = new HttpClient();

            _container
                .Use(logger)
                .Use(client)
                .RegisterCommand<RandomCatCommand>()
                .RegisterCommand<RandomDogCommand>()
                //.Register<RandomFoxCommand>()
                .RegisterCommand<AddNumbersCommand>();


            var c = await new CloudCommands().InitializeAsync(_container, new AzureStorageQueuesConnectionOptions(Configuration["StorageAccountName"], Configuration["StorageAccountKey"], 3, logger, QueueNamePrefix: _queueNamePrefix));


            _ = await c.PostCommandAsync<RandomCatCommand>(new { Name = "Laika" });

            _ = await c.PostCommandAsync<RandomDogCommand>(new { Name = "Scooby-Doo" });

            _ = await c.PostCommandAsync<RandomFoxCommand>(new { Name = "Penny" });

            _ = await c.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });


            var result = await c.ExecuteCommandsAsync();


            //check if something was wrong or if any items were processed at all
            Assert.IsTrue(!result.Item1); 

            //check if 1 or more items were processed
            Assert.IsTrue(result.Item2 > 0);

            //check if there was any errors
            Assert.IsTrue(result.Item3.Count > 0); //This value keeps the list of error messages that were encountered. The format of the messages is "{MessageId}:{Exception Messgae}". After retrying x amount of times the message is moved to the deadletterqueue (see code for how this is implemented)

        }

        //[TestMethod]
        //public async Task TestingQueuePostAndRetriveCommands()
        //{
        //    var logger = new DebugLoggerProvider().CreateLogger("default");
        //    using var client = new HttpClient();

        //    _container
        //        .Use(logger)
        //        .Use(client)
        //        .RegisterCommand<RandomCatCommand>()
        //        .RegisterCommand<RandomDogCommand>()
        //        .RegisterCommand<RandomFoxCommand>()
        //        .RegisterCommand<AddNumbersCommand>();

        //    var c = new Commands(_container, Configuration["StorageAccounName"], Configuration["StorageAccountKey"], logger, QueueNamePrefix: "commands-test");

        //    c.AddToQueue<RandomCatCommand>(new { Name = "Laika" });
        //    c.AddToQueue<RandomDogCommand>(new { Name = "Scooby-Doo" });
        //    c.AddToQueue<RandomFoxCommand>(new { Name = "Penny" });

        //    await c.FlushQueueAsync();

        //    var result = await c.ExecuteCommands();

        //    //check if something was wrong or if any items were processed at all
        //    Assert.IsTrue(result.Item1); 

        //    //check if 1 or more items were processed
        //    Assert.IsTrue(result.Item2 > 0 && result.Item2 == 3); 

        //    //check if there was any errors
        //    Assert.IsTrue(result.Item3.Count == 0); 

        //}

       

    }
}
