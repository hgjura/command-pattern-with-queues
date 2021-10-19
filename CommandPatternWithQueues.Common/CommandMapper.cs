using System;
using System.Collections.Generic;

namespace CommandPatternWithQueues.Common
{
    public class CommandMapper
    {
        #region boilerplate
        static Dictionary<string, Type> typemap;
        static CommandMapper()
        {
            typemap = new Dictionary<string, Type>();

            Map();
        }
        public CommandMapper Register(string key, Type value)
        {
            typemap.Add(key, value);
            return this;
        }

        public Type Get(string key)
        {
            return typemap[key];
        }
        #endregion

        private static void Map()
        {
            typemap.Add(nameof(RandomCatCommand), typeof(RandomCatCommand));
            typemap.Add(nameof(RandomDogCommand), typeof(RandomDogCommand));
            typemap.Add(nameof(RandomFoxCommand), typeof(RandomFoxCommand));
            typemap.Add(nameof(AddNumbersCommand), typeof(AddNumbersCommand));
        }

        
    }
}
