using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using ServerTools.ServerCommands;
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

        public bool RequiresResponse => true;

        public async Task<(bool, Exception, dynamic, dynamic)> ExecuteAsync(dynamic command, dynamic meta)
        {
            logger ??= new DebugLoggerProvider().CreateLogger("default");

            try
            {

                int n1 = (int)command.Number1;
                int n2 = (int)command.Number2;

                int result = n1 + n2;

                logger.LogInformation($"<< {n1} + {n2} = {n1 + n2} >>");

                return await Task.FromResult<(bool, Exception, dynamic, dynamic)>((true, null, new { Result =  result, Message = "Ok." }, meta));


            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);

                return await Task.FromResult<(bool, Exception, dynamic, dynamic)>((false, ex, null, meta));
            }
            finally
            {
                
            }
        }

    }


}
