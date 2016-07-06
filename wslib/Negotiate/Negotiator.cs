using System.IO;
using System.Threading.Tasks;
using wslib.Protocol;
using wslib.Utils;

namespace wslib.Negotiate
{
    internal class Negotiator
    {
        private readonly HandshakeOptions options;
        private readonly WsHandshake handshaker;

        public Negotiator(HandshakeOptions options)
        {
            this.options = options;
            var deflateExtension = new DeflateExtension.DeflateExtension();
            handshaker = new WsHandshake(new HttpParser(), new HttpComposer(), new[] { deflateExtension }, options.HttpHook);
        }

        public async Task<WebSocket> Negotiate(Stream clientStream)
        {
            var handshake = await handshaker.Performhandshake(clientStream).WithTimeout(options.NegotiationTimeout).ConfigureAwait(false);
            return new WebSocket(handshake.Env, handshake.Stream, handshake.Extensions, true);
        }
    }
}