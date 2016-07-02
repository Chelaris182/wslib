using System;
using System.Net;
using wslib.Negotiate;

namespace wslib
{
    public class WebSocketListenerOptions
    {
        public IPEndPoint Endpoint = new IPEndPoint(IPAddress.Any, 80);
        public int TcpBacklog = int.MaxValue;
        public int MaxOutstandingAccepts = 5 * Environment.ProcessorCount;
        public int MaxOutstandingRequests = int.MaxValue;
        public NegotiateOptions NegotiateOptions = new NegotiateOptions();
    }
}