using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public class RandomFoxCommand : WebCommandBase
    {
        public override async Task<(bool, Exception)> ExecuteAsync(dynamic command, dynamic metadata, ILogger log = null, HttpClient client = null)
        {
            logger = log ?? new DebugLoggerProvider().CreateLogger("default");
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

                return (true, null);

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return (false, ex);
            }
        }
    }
}
