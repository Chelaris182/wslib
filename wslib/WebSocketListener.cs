﻿using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using wslib.Negotiate;
using wslib.Protocol;

namespace wslib
{
    public class WebSocketListener : IDisposable
    {
        private static readonly TimeSpan defaultPingPeriod = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan defaultInactivityTimeout = TimeSpan.FromSeconds(10);

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
            negotiator = new Negotiator(options.HandshakeOptions);
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
                socket.NoDelay = true;

                Stream stream = new NetworkStream(socket, true);
                try
                {
                    offloadStartNextRequest();

                    if (options.UseSSL)
                    {
                        var ssl = new SslStream(stream);
                        stream = ssl;
                        await ssl.AuthenticateAsServerAsync(options.certificate).ConfigureAwait(false);
                    }

                    await processRequestAsync(stream).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref currentOutstandingRequests);
                    stream.Dispose();
                }
            }
        }

        private async Task processRequestAsync(Stream stream)
        {
            using (WebSocket ws = await negotiator.Negotiate(stream).ConfigureAwait(false))
            {
                Heartbit.RunHeartbit(ws, defaultPingPeriod, defaultInactivityTimeout); // fire&forget // TODO: log errors
                await appFunc(ws).ConfigureAwait(false);
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