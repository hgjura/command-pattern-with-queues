using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommandPatternWithQueues.Common
{
    public interface IRemoteResponse
    {
        /// <summary>
        /// Executes the remote response of the comamnd.
        /// </summary>
        /// <param name="response">The body of the response command. The properties of this dynamic parameter must be known to the inner function for each instance class. They will most likely be different for each class that derives from IRemoteResponse</param>
        /// <param name="metadata">The metadata of the response command. The properties of this dynamic parameter must be known to the inner function for each instance class.</param>
        /// <returns>A tupple response 32-bit positive integer, representing the sum of the two specified numbers.
        /// <list type="bullet">
        /// <item>
        /// <description>Item1. A bool that inidcates if the remote Response executed succesfully or not.</description>
        /// </item>
        /// <item>
        /// <description>Item2. The excpetion object, in the case when Item1 is false, otherwise null.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Task<(bool, Exception)> ExecuteAsync(dynamic response, dynamic metadata);
    }
}
