using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public abstract class WebCommandBase : CommandBase
    {
        public abstract Task<(bool, Exception)> ExecuteAsync(dynamic command, ILogger logger = null, HttpClient client = null);
    }
}
