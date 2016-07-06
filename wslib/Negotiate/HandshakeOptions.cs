using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wslib.Negotiate
{
    public class HandshakeOptions
    {
        public TimeSpan NegotiationTimeout = TimeSpan.FromSeconds(10);
        public Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task> HttpHook;
    }
}