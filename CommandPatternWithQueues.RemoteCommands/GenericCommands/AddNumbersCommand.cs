using CommandPatternWithQueues.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.RemoteCommands
{
    public class AddNumbersCommand : IRemoteCommand
    {
        private ILogger logger;
        public AddNumbersCommand(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<(bool, Exception)> ExecuteAsync(dynamic command, dynamic meta)
        {
            logger ??= new DebugLoggerProvider().CreateLogger("default");

            try
            {

                int n1 = (int)command.Number1;
                int n2 = (int)command.Number2;

                logger.LogInformation($"<< {n1} + {n2} = {n1 + n2} >>");

                return await Task.FromResult<(bool, Exception)>((true, null));


            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                
                return await Task.FromResult<(bool, Exception)>((false, ex));
            }

            
        }
    }
}
