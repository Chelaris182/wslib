using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using wslib.Negotiate.Extensions;
using wslib.Protocol;

namespace wslib.Negotiate
{
    public class WsHandshake
    {
        private readonly Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task> negotiationHook;
        private readonly IHttpParser httpParser;
        private readonly IHttpComposer httpComposer;
        private readonly IEnumerable<IServerExtension> serverExtensions;

        public WsHandshake(IHttpParser httpParser,
                           IHttpComposer httpComposer,
                           IEnumerable<IServerExtension> extensions,
                           Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task> negotiationHook)
        {
            this.negotiationHook = negotiationHook;
            this.httpParser = httpParser;
            this.httpComposer = httpComposer;
            this.serverExtensions = extensions;
        }

        public async Task<HandshakeResult> Performhandshake(Stream stream)
        {
            try
            {
                HttpRequest httpRequest = await httpParser.ParseHttpRequest(stream).ConfigureAwait(false);
                validateRequest(httpRequest);
                HttpResponse httpResponse = new HttpResponse();
                List<IMessageExtension> extensions = negotiateExtensions(httpRequest, httpResponse);
                var env = new Dictionary<string, object>();
                if (negotiationHook != null)
                    await negotiationHook(httpRequest, httpResponse, env).ConfigureAwait(false);
                negotiate(httpRequest, httpResponse);
                await httpComposer.WriteResponse(httpResponse, stream).ConfigureAwait(false);
                return new HandshakeResult(env, extensions, stream);
            }
            catch (HandshakeException)
            {
                HttpResponse httpResponse = new HttpResponse { Status = HttpStatusCode.BadRequest }; // TODO: send body? add a test
                await httpComposer.WriteResponse(httpResponse, stream).ConfigureAwait(false);
                throw;
            }
        }

        private List<IMessageExtension> negotiateExtensions(HttpRequest httpRequest, HttpResponse httpResponse)
        {
            if (!serverExtensions.Any()) return new List<IMessageExtension>();

            string value;
            if (httpRequest.Headers.TryGetValue("Sec-WebSocket-Extensions", out value))
            {
                IEnumerable<ExtensionRequest> clientExtensions = HandshakeExtensions.ParseExtensionHeader(value);

                var matchedExtensions = new List<ExtensionRequest>();
                var messageExtensions = new List<IMessageExtension>();
                foreach (var clientE in clientExtensions)
                {
                    foreach (var serverE in serverExtensions)
                    {
                        ExtensionParams matchedParams;
                        IMessageExtension messageExtension;
                        if (serverE.TryMatch(clientE.Token, clientE.Params, out matchedParams, out messageExtension))
                        {
                            matchedExtensions.Add(new ExtensionRequest(clientE.Token, matchedParams));
                            messageExtensions.Add(messageExtension);
                            break;
                        }
                    }
                }

                if (messageExtensions.Count == 0) return messageExtensions;

                string header = HandshakeExtensions.ComposeExtensionHeader(matchedExtensions);
                httpResponse.Headers["Sec-WebSocket-Extensions"] = header;
                return messageExtensions;
            }

            return null;
        }

        private void negotiate(HttpRequest httpRequest, HttpResponse httpResponse)
        {
            if (httpResponse.Status != HttpStatusCode.SwitchingProtocols) return;

            string value;
            if (httpRequest.Headers.TryGetValue("Sec-WebSocket-Protocol", out value))
            {
                if (!httpResponse.Headers.ContainsKey("Sec-WebSocket-Protocol"))
                {
                    httpResponse.Headers.Add("Sec-WebSocket-Protocol", value);
                }
            }

            value = httpRequest.Headers["Sec-WebSocket-Key"];
            SHA1 sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(value + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
            var acceptKey = Convert.ToBase64String(sha1.ComputeHash(bytes));
            httpResponse.Headers.Add("Sec-WebSocket-Accept", acceptKey);
        }

        private void validateRequest(HttpRequest httpRequest)
        {
            string value;
            if (!httpRequest.Headers.ContainsKey("Host"))
                throw new HandshakeException("no or bad Host header");
            if (!httpRequest.Headers.TryGetValue("Upgrade", out value) || !value.Equals("websocket", StringComparison.InvariantCultureIgnoreCase))
                throw new HandshakeException("no or bad Upgrade header");
            if (!httpRequest.Headers.TryGetValue("Connection", out value) || !value.Equals("Upgrade", StringComparison.InvariantCultureIgnoreCase))
                throw new HandshakeException("no or bad Connection header");
            if (!httpRequest.Headers.ContainsKey("Sec-WebSocket-Key"))
                throw new HandshakeException("no Sec-WebSocket-Key header");
            if (!httpRequest.Headers.TryGetValue("Sec-WebSocket-Version", out value) || !value.Equals("13"))
                throw new HandshakeException("no or bad Sec-WebSocket-Version header");
        }
    }
}