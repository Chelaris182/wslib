using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using wslib.Negotiate;

namespace wslib
{
    public class WebSocketListener : IDisposable
    {
        private readonly WebSocketListenerOptions options;
        private readonly Func<IWebSocket, Task> appFunc;
        private readonly TcpListener listener;
        private readonly Negotiator negotiator;
        private int currentOutstandingAccepts;
        private int currentOutstandingRequests;

        public WebSocketListener(WebSocketListenerOptions options, Func<IWebSocket, Task> appFunc)
        {
            this.options = options;
            this.appFunc = appFunc;
            listener = new TcpListener(options.Endpoint);
            negotiator = new Negotiator(options.NegotiateOptions);
        }

        public void StartAccepting()
        {
            listener.Start(options.TcpBacklog);
            offloadStartNextRequest();
        }

        private async Task startNextRequest()
        {
            while (true)
            {
                Interlocked.Increment(ref currentOutstandingAccepts);
                Socket socket;
                try
                {
                    socket = await listener.AcceptSocketAsync().ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref currentOutstandingAccepts);
                }

                Interlocked.Increment(ref currentOutstandingRequests);
                var stream = new NetworkStream(socket, FileAccess.ReadWrite, true);
                try
                {
                    offloadStartNextRequest();
                    await processRequestAsync(stream).ConfigureAwait(false);
                }
                finally
                {
                    stream.Dispose();
                    Interlocked.Decrement(ref currentOutstandingRequests);
                }
            }
        }

        private async Task processRequestAsync(Stream stream)
        {
            IWebSocket ws = await negotiator.Negotiate(stream).ConfigureAwait(false);
            try
            {
                await appFunc(ws).ConfigureAwait(false);
            }
            finally
            {
                ws.Dispose();
            }
        }

        private bool CanAcceptMoreRequests => (currentOutstandingAccepts < options.MaxOutstandingAccepts
                                               && currentOutstandingRequests < options.MaxOutstandingRequests - currentOutstandingAccepts);

        private void offloadStartNextRequest()
        {
            if (CanAcceptMoreRequests)
            {
                Task.Run(startNextRequest); // TODO: log errors
            }
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}