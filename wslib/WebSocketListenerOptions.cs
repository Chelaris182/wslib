using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
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
        public bool UseSSL = false;
        public X509Certificate2 certificate;
    }
}