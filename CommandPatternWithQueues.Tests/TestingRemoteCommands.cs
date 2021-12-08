using CommandPatternWithQueues.RemoteCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerTools.ServerCommands;
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
        public static void InitTestSuite(TestContext testContext)
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<TestingRemoteCommands>(true)
                .AddJsonFile("local.tests.settings.json", true);

            Configuration = builder.Build();

            _container = new CommandContainer();
            _queueNamePrefix = nameof(TestingRemoteCommands).ToLower();
            _ = new Commands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], null, QueueNamePrefix: _queueNamePrefix);
        }

        [ClassCleanup()]
        public static void CleanTestSuite()
        {
            new Commands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], null, QueueNamePrefix: _queueNamePrefix).Clear(true);
        }



        [TestMethod]
        public void A1000_TestingSimplePostAndRetriveCommands()
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
            

            var c = new Commands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], logger, QueueNamePrefix: _queueNamePrefix);

            _ = c.PostCommand<RandomCatCommand>(new { Name = "Laika" }).GetAwaiter().GetResult();

            _ = c.PostCommand<RandomDogCommand>(new { Name = "Scooby-Doo" }).GetAwaiter().GetResult();

            _ = c.PostCommand<RandomFoxCommand>(new { Name = "Penny" }).GetAwaiter().GetResult();

            _ = c.PostCommand<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 }).GetAwaiter().GetResult();

            var result1 = c.ExecuteCommands().GetAwaiter().GetResult();

            var result2 = c.ExecuteResponses().GetAwaiter().GetResult();

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


            var c = new Commands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], logger, QueueNamePrefix: _queueNamePrefix);

            _ = await c.PostCommand<RandomCatCommand>(new { Name = "Laika" });

            _ = await c.PostCommand<RandomDogCommand>(new { Name = "Scooby-Doo" });

            _ = await c.PostCommand<RandomFoxCommand>(new { Name = "Penny" });

            _ = await c.PostCommand<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });


            var result = await c.ExecuteCommands();


            //check if something was wrong or if any items were processed at all
            Assert.IsTrue(!result.Item1); 

            //check if 1 or more items were processed
            Assert.IsTrue(result.Item2 > 0);

            //check if there was any errors
            Assert.IsTrue(result.Item3.Count > 0); //This value keeps the list of error messages that were encountered. The format of the messages is "{MessageId}:{Exception Messgae}". After retrying x amount of times the message is moved to the deadletterqueue (see code for how this is implemented)

        }

        [TestMethod]
        public async Task TestingQueuePostAndRetriveCommands()
        {
            var logger = new DebugLoggerProvider().CreateLogger("default");
            using var client = new HttpClient();

            _container
                .Use(logger)
                .Use(client)
                .RegisterCommand<RandomCatCommand>()
                .RegisterCommand<RandomDogCommand>()
                .RegisterCommand<RandomFoxCommand>()
                .RegisterCommand<AddNumbersCommand>();

            var c = new Commands(_container, Configuration["StorageAccounName"], Configuration["StorageAccountKey"], logger, QueueNamePrefix: "commands-test");

            c.AddToQueue<RandomCatCommand>(new { Name = "Laika" });
            c.AddToQueue<RandomDogCommand>(new { Name = "Scooby-Doo" });
            c.AddToQueue<RandomFoxCommand>(new { Name = "Penny" });

            await c.FlushQueueAsync();

            var result = await c.ExecuteCommands();

            //check if something was wrong or if any items were processed at all
            Assert.IsTrue(result.Item1); 

            //check if 1 or more items were processed
            Assert.IsTrue(result.Item2 > 0 && result.Item2 == 3); 

            //check if there was any errors
            Assert.IsTrue(result.Item3.Count == 0); 

        }

       

    }
}
