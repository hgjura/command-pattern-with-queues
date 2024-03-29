﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServerTools.ServerCommands;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.RemoteCommands
{
    public class RandomCatCommand : IRemoteCommand
    {
        private ILogger logger;
        private HttpClient client;

        public RandomCatCommand(ILogger logger, HttpClient client)
        {
            this.logger = logger;
            this.client = client;
        }
        public bool RequiresResponse => false;
        public async Task<(bool, Exception, dynamic, CommandMetadata)> ExecuteAsync(dynamic command, CommandMetadata metadata)
        {
            logger ??= new DebugLoggerProvider().CreateLogger("default");
            var api = "https://api.thecatapi.com/v1/images/search?format=json";


            async Task<JArray> getdata(HttpClient c, string api)
            {
                var response = (await client.GetAsync(api)).EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JArray>(responseBody);
            };

            async Task<JArray> getdatawithdisposableclient(string api)
            {
                using var c = new HttpClient();
                var response = (await c.GetAsync(api)).EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JArray>(responseBody);
            };


            try
            {
                var json = client != null ? await getdata(client, api) : await getdatawithdisposableclient(api);

                var url = json[0]["url"].ToString();
                var name = (string)command.Name;
                
                logger.LogInformation($"<< New random cat by name of {name} retrieved.Check it out here: {url} >>");
                
                return (true, null, null, metadata);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return (false, ex, null, metadata);
            }
        }
    }
}
