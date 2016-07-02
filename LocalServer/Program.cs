using System;
using System.IO;
using System.Net;
using System.Text;
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
            throw new NotImplementedException(); // TODO: log
        }

        private static async Task appFunc(IWebSocket webSocket)
        {
            while (webSocket.IsConnected())
            {
                using (var msg = await webSocket.ReadMessageAsync(CancellationToken.None))
                {
                    if (msg != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await msg.ReadStream.CopyToAsync(ms);
                            var text = Encoding.UTF8.GetString(ms.ToArray());

                            using (var w = await webSocket.CreateWriter(MessageType.Text, CancellationToken.None))
                            {
                                await w.WriteFinalAsync(ms.ToArray(), 0, (int)ms.Length, CancellationToken.None);
                            }
                        }
                    }
                }
            }
        }
    }
}