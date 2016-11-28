using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using wslib;
using wslib.Protocol;

namespace LocalServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += LogUnobservedTaskException;

            var listenerOptions = new WebSocketListenerOptions { Endpoint = new IPEndPoint(IPAddress.Loopback, 8080) };
            using (var listener = new WebSocketListener(listenerOptions, appFunc))
            {
                listener.StartAccepting();
                Console.ReadLine();
            }
        }

        private static void LogUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            //Console.WriteLine(unobservedTaskExceptionEventArgs.Exception);
        }

        private static async Task appFunc(IWebSocket webSocket)
        {
            Task wrt = null;
            while (webSocket.IsConnected())
            {
                using (var msg = await webSocket.ReadMessageAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    if (msg == null) continue;

                    using (var ms = new MemoryStream())
                    {
                        await msg.ReadStream.CopyToAsync(ms).ConfigureAwait(false);
                        byte[] array = ms.ToArray();
                        if (wrt != null) await wrt.ConfigureAwait(false);
                        wrt = pushMessage(webSocket, msg, array);
                    }
                }
            }
        }

        private static async Task pushMessage(IWebSocket webSocket, WsMessage msg, byte[] array)
        {
            using (var w = await webSocket.CreateMessageWriter(msg.Type, CancellationToken.None).ConfigureAwait(false))
            {
                await w.WriteMessageAsync(array, 0, array.Length, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}