using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using wslib.Protocol;
using WebSocket = wslib.Protocol.WebSocket;

namespace wslib.Negotiate
{
    internal class Negotiator
    {
        private readonly NegotiateOptions options;
        private readonly WsHandshake handshaker;

        public Negotiator(NegotiateOptions options)
        {
            this.options = options;
            handshaker = new WsHandshake(this.options.HttpHook);
        }

        public async Task<WebSocket> Negotiate(Stream clientStream)
        {
            var timeoutTask = Task.Delay(options.NegotiationTimeout);
            var handshakeTask = handshaker.Performhandshake(clientStream);
            await Task.WhenAny(timeoutTask, handshakeTask).ConfigureAwait(false);
            if (timeoutTask.IsCompleted)
                throw new WebSocketException("Negotiation timeout");

            var handshake = await handshakeTask.ConfigureAwait(false);
            return new WebSocket(handshake.Env, handshake.Stream);
        }
    }
}