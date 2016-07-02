using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wslib.Models;

namespace wslib.Negotiate
{
    public class NegotiateOptions
    {
        public TimeSpan NegotiationTimeout = TimeSpan.FromSeconds(10);
        public Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task> HttpHook;
    }
}