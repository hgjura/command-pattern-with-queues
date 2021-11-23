using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public class AddNumbersCommand : GenericCommandBase
    {

        public override async Task<(bool, Exception)> ExecuteAsync(dynamic command, dynamic meta, ILogger log = null)
        {
            logger = log ?? new DebugLoggerProvider().CreateLogger("default");

            try
            {

                int n1 = (int)command.Number1;
                int n2 = (int)command.Number2;

                logger.LogInformation($"<< {n1} + {n2} = {n1 + n2} >>");

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
