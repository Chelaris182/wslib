using System;
using System.Collections;
using System.Collections.Generic;

namespace wslib.Negotiate.Extensions
{
    public class ExtensionParams : IEnumerable<Tuple<string, string>>
    {
        private readonly List<Tuple<string, string>> p = new List<Tuple<string, string>>();

        public void Add(string name)
        {
            Add(name, string.Empty);
        }

        public void Add(string name, string value)
        {
            p.Add(Tuple.Create(name, value));
        }

        public IEnumerator<Tuple<string, string>> GetEnumerator()
        {
            return p.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class ExtensionRequest
    {
        public readonly string Token;
        public readonly ExtensionParams Params;

        public ExtensionRequest(string token, ExtensionParams parameters)
        {
            Token = token;
            Params = parameters;
        }
    }
}