﻿using CommandPatternWithQueues.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.RemoteCommands
{
    public class AddNumbersResponse : IRemoteResponse
    {
        private ILogger logger;
        public AddNumbersResponse(ILogger logger)
        {
            this.logger = logger;
        }
        public async Task<(bool, Exception)> ExecuteAsync(dynamic response, dynamic metadata)
        {
            logger ??= new DebugLoggerProvider().CreateLogger("default");

            try
            {

                var r = (int)response.Result;
                var m = (string)response.Message;

                logger.LogInformation($"<< Result from the command is in: Result = {r} | Message = {m} >>");

                return await Task.FromResult<(bool, Exception)>((true, null));


            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);

                return await Task.FromResult<(bool, Exception)>((false, ex));
            }
            finally
            {

            }
        }
    }
}