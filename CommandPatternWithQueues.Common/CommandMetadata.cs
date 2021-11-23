using System;
using System.Collections.Generic;
using System.Text;

namespace CommandPatternWithQueues.Common
{
    public class CommandMetadata
    {
        public Guid CorrelationId { get; internal set; }
    }
}
