using CommandPatternWithQueues.Common;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestingSimplePostAndRetriveCommands()
        {
            string _STORAGE_CONNECTIONSTRING = Environment.GetEnvironmentVariable("StorageConnectionString");
            
            using var client = new HttpClient();

            var c = new Commands(_STORAGE_CONNECTIONSTRING, client, new DebugLoggerProvider().CreateLogger("default"));

            _ = await c.PostCommand<RandomCatCommand>(new { Name = "Laika" });

            _ = await c.PostCommand<RandomDogCommand>(new { Name = "Scooby-Doo" });

            _ = await c.PostCommand<RandomFoxCommand>(new { Name = "Penny" });

            _ = await c.PostCommand<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });


            var result = await c.GetCommands();

            //check if something was wrion or if any items were processed at all
            Assert.IsTrue(result.Item1); //This value will return true if any items were processed AND there was no errors

            //check if 1 or more items were processed
            Assert.IsTrue(result.Item2 > 0 && result.Item2 == 4); //This value keeps the amount of commands that were processed 

            //check if there was any errors
            Assert.IsTrue(result.Item3.Count == 0); //This value keeps the list of error messages that were encountered. The format of the messages is "{MessageId}:{Exception Messgae}". After retrying x amount of times the message is moved to the deadletterqueue (see code for how this is implemented)

        }
    }
}
