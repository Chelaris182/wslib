using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    internal class Heartbit
    {
        public static async Task RunHeartbit(WebSocket socket)
        {
            while (socket.IsConnected())
            {
                var now = DateTime.Now;
                if (socket.LastActivity.Add(TimeSpan.FromSeconds(10)) < now)
                {
                    await socket.CloseAsync(CloseStatusCode.ProtocolError, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var pingTime = socket.LastActivity.Add(TimeSpan.FromSeconds(5));
                var toSleep = pingTime - now;
                if (pingTime < now)
                {
                    await sendPing(socket).ConfigureAwait(false);
                    toSleep = TimeSpan.FromSeconds(5);
                }

                await Task.Delay(toSleep).ConfigureAwait(false);
            }
        }

        private static Task sendPing(WebSocket socket)
        {
            return socket.SendMessage(WsFrameHeader.Opcodes.PING, new byte[] { 0x00 }, CancellationToken.None);
        }
    }
}