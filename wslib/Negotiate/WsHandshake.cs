using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using wslib.Models;

namespace wslib.Negotiate
{
    public class WsHandshake
    {
        private readonly Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task> negotiationHook;
        private readonly IHttpParser httpParser;
        private readonly IHttpComposer httpComposer;

        public WsHandshake(Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task> negotiationHook) :
            this(negotiationHook, new HttpParser(), new HttpComposer())
        {
        }

        public WsHandshake(Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task> negotiationHook, IHttpParser httpParser, IHttpComposer httpComposer)
        {
            this.negotiationHook = negotiationHook;
            this.httpParser = httpParser;
            this.httpComposer = httpComposer;
        }

        public async Task<HandshakeResult> Performhandshake(Stream stream)
        {
            HttpRequest httpRequest = await httpParser.ParseHttpRequest(stream).ConfigureAwait(false);
            validateRequest(httpRequest);
            HttpResponse httpResponse = new HttpResponse();
            var env = new Dictionary<string, object>();
            // TODO: modify stream with extensions here
            if (negotiationHook != null)
                await negotiationHook(httpRequest, httpResponse, env).ConfigureAwait(false);
            negotiate(httpRequest, httpResponse);
            await httpComposer.WriteResponse(httpResponse, stream).ConfigureAwait(false);
            return new HandshakeResult(env, stream);
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
            if (!httpRequest.Headers.TryGetValue("Upgrade", out value) || !value.Equals("websocket", StringComparison.InvariantCultureIgnoreCase))
                throw new HandshakeException("no or bad upgrade header");
            if (!httpRequest.Headers.ContainsKey("Sec-WebSocket-Key"))
                throw new HandshakeException("no sec-websocket-key header");
            if (!httpRequest.Headers.TryGetValue("Sec-WebSocket-Version", out value) || !value.Equals("13"))
                throw new HandshakeException("no or bad sec-websocket-version header");
        }
    }
}