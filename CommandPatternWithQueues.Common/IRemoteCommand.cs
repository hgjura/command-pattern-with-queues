using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public interface IRemoteCommand
    {
        public Task<(bool, Exception)> ExecuteAsync(dynamic command, dynamic metadata);
    }
}
