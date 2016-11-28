using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    public static class Heartbit
    {
        private static readonly ArraySegment<byte> pingPayload = new ArraySegment<byte>(new byte[] { 0x00 });

        public static async Task RunHeartbit(IWebSocket socket, TimeSpan pingPeriod, TimeSpan inactivityPeriod)
        {
            while (socket.IsConnected())
            {
                var now = DateTime.Now;
                if (socket.LastActivity().Add(inactivityPeriod) < now)
                {
                    await socket.CloseAsync(CloseStatusCode.ProtocolError, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var pingTime = socket.LastActivity().Add(pingPeriod);
                var toSleep = pingTime - now;
                if (pingTime < now)
                {
                    await sendPing(socket).ConfigureAwait(false);
                    toSleep = pingPeriod;
                }

                await Task.Delay(toSleep).ConfigureAwait(false);
            }
        }

        private static async Task sendPing(IWebSocket socket)
        {
            await socket.SendMessageAsync(WsFrameHeader.Opcodes.PING, pingPayload, CancellationToken.None).ConfigureAwait(false);
        }
    }
}