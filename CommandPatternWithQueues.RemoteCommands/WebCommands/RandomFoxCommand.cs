using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServerTools.ServerCommands;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.RemoteCommands
{
    public class RandomFoxCommand : IRemoteCommand
    {
        private ILogger logger;
        private HttpClient client;

        public RandomFoxCommand(ILogger logger, HttpClient client)
        {
            this.logger = logger;
            this.client = client;
        }
        public bool RequiresResponse => false;
        public async Task<(bool, Exception, dynamic, dynamic)> ExecuteAsync(dynamic command, dynamic metadata)
        {
            logger ??= new DebugLoggerProvider().CreateLogger("default");
            var api = "https://randomfox.ca/floof";

            async Task<JObject> getdata(HttpClient c, string api)
            {
                var response = (await client.GetAsync(api)).EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JObject>(responseBody);
            };

            async Task<JObject> getdatawithdisposableclient(string api)
            {
                using var c = new HttpClient();
                var response = (await c.GetAsync(api)).EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JObject>(responseBody);
            };


            try
            {
                var json = client != null ? await getdata(client, api) : await getdatawithdisposableclient(api);

                var url = json["image"].ToString();
                var name = (string)command.Name;
                
                logger.LogInformation($"<< New random fox by name of {name} retrieved. Check it out here: {url} >>");

                return (true, null, null, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return (false, ex, null, null);
            }
        }
    }
}
