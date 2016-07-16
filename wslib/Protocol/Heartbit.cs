using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    internal class Heartbit
    {
        private static readonly ArraySegment<byte> pingPayload = new ArraySegment<byte>(new byte[] { 0x00 });

        public static async Task RunHeartbit(WebSocket socket)
        {
            while (socket.IsConnected())
            {
                var now = DateTime.Now;
                if (socket.LastActivity.Add(TimeSpan.FromSeconds(10)) < now) // TODO: make timeout configurable
                {
                    await socket.CloseAsync(CloseStatusCode.ProtocolError, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var pingTime = socket.LastActivity.Add(TimeSpan.FromSeconds(5)); // TODO: make timeout configurable
                var toSleep = pingTime - now;
                if (pingTime < now)
                {
                    await sendPing(socket).ConfigureAwait(false);
                    toSleep = TimeSpan.FromSeconds(5);
                }

                await Task.Delay(toSleep).ConfigureAwait(false);
            }
        }

        private static async Task sendPing(WebSocket socket)
        {
            using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            {
                await socket.SendMessage(WsFrameHeader.Opcodes.PING, pingPayload, tokenSource.Token).ConfigureAwait(false);
            }
        }
    }
}