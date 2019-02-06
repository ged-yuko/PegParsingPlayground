using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;

namespace System
{
    //TODO : burn it with fire
    internal class MyEnvironment
    {
        static readonly Dictionary<string, string> _strings;

        static MyEnvironment()
        {
            var type = typeof(int);
            var reader = new ResourceReader(type.Assembly.GetManifestResourceStream("mscorlib.resources"));

            _strings = new Dictionary<string, string>(
                reader.Cast<DictionaryEntry>()
                      .Where(kv => (kv.Key is string) && (kv.Value is string))
                      .ToDictionary(kv => kv.Key as string, kv => kv.Value as string)
            );
        }

        public static string GetResourceString(string key)
        {
            return _strings[key];
        }
    }
}
