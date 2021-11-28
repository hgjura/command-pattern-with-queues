using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public interface IRemoteCommand
    {
        /// <summary>
        /// Executes the remote command.
        /// </summary>
        /// <param name="command">The body of the command. The properties of this dynamic parameter must be known to the inner function for each instance class. They will most likely be different for each class that derives from IRemoteResponse</param>
        /// <example>For example, for a remote Command that adds to integer, the body of the command may look like this:
        /// <code>
        ///    new { 
        ///    Number1 = 5, 
        ///    Number2 = 6 
        ///    };
        /// </code>
        /// in this case, the properties Number1 and Number2 must be known to the calli instance. If the instance tries to retrieve a property that is not ine the command body a propertyNotFound exception of teh dynamic object will be thrown.
        /// </example>
        /// <param name="metadata">The metadata of the response command. The properties of this dynamic parameter must be known to the inner function for each instance class.</param>
        /// <returns>A tupple response 32-bit positive integer, representing the sum of the two specified numbers.
        /// <list type="bullet">
        /// <item>
        /// <description>Item1. A bool that inidcates if the remote Response executed succesfully or not.</description>
        /// </item>
        /// <item>
        /// <description>Item2. The excpetion object, in the case when Item1 is false, otherwise null.</description>
        /// <item>
        /// <description>Item3. In the case when <c>RequiresResponse</c> property is set to true, this will hold the body of the response, otherwise null. <see cref="IRemoteResponse.ExecuteAsync(dynamic, dynamic)"/>.</description>
        /// <item>
        /// <description>Item3. In the case when <c>RequiresResponse</c> property is set to true, this will hold the metadata of the response, otherwise null. <see cref="IRemoteResponse.ExecuteAsync(dynamic, dynamic)"/>. Generally this is the metadata of the original command with added poperties during Response creation and execution.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Task<(bool, Exception, dynamic, dynamic)> ExecuteAsync(dynamic command, dynamic metadata);
        public bool RequiresResponse { get; }
    }
}
