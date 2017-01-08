using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using wslib.DeflateExtension;
using wslib.Negotiate;
using wslib.Protocol;

namespace UnitTests
{
    public class HandshakeTests
    {
        // TODO: add a test for negotiation timeout
        private HttpRequest httpRequest;
        private WsHandshake wsHandshake;
        private HttpResponse spyedResponse;
        private readonly Mock<IHttpParser> parser = new Mock<IHttpParser>();
        private readonly Mock<IHttpComposer> composer = new Mock<IHttpComposer>();

        [SetUp]
        public void Setup()
        {
            httpRequest = prepareValidHttpRequest();
            parser.Setup(httpParser => httpParser.ParseHttpRequest(Stream.Null)).ReturnsAsync(httpRequest);

            composer
                .Setup(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null))
                .Returns(() => Task.FromResult(true))
                .Callback<HttpResponse, Stream>((response, _) => { spyedResponse = response; });

            wsHandshake = new WsHandshake(parser.Object, composer.Object, Enumerable.Empty<IServerExtension>(), null);
        }

        // Request requirements https://tools.ietf.org/html/rfc6455#section-4.1
        [Test]
        [TestCase("Host")]
        [TestCase("Upgrade")]
        [TestCase("Connection")]
        [TestCase("Sec-WebSocket-Key")]
        [TestCase("Sec-WebSocket-Version")]
        public void MissingHeader(string header)
        {
            httpRequest.Headers.Remove(header);
            Assert.ThrowsAsync<HandshakeException>(() => wsHandshake.Performhandshake(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task Success()
        {
            var handShakeResult = await wsHandshake.Performhandshake(Stream.Null);

            composer.Verify(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null));
            Assert.That(handShakeResult.Stream, Is.EqualTo(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.SwitchingProtocols));
            Assert.That(spyedResponse.Headers["Sec-WebSocket-Accept"], Is.EqualTo("s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));
            Assert.That(spyedResponse.Headers.ContainsKey("Sec-WebSocket-Extensions"), Is.False);
        }

        [TestCase("permessage-deflate")]
        [TestCase("unknown-extension, permessage-deflate")]
        [TestCase("permessage-deflate, unknown-extension")]
        [TestCase("unknown-extension; arg1=value1, permessage-deflate; client_context_takeover")]
        public async Task SuccessExtensions(string extensionList)
        {
            httpRequest.Headers["Sec-WebSocket-Extensions"] = extensionList;
            var serverExtensions = new IServerExtension[] { new DeflateExtension() };
            wsHandshake = new WsHandshake(parser.Object, composer.Object, serverExtensions, null);
            var handShakeResult = await wsHandshake.Performhandshake(Stream.Null);

            composer.Verify(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null));
            Assert.That(handShakeResult.Stream, Is.EqualTo(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.SwitchingProtocols));
            Assert.That(spyedResponse.Headers["Sec-WebSocket-Accept"], Is.EqualTo("s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));
            Assert.That(spyedResponse.Headers["Sec-WebSocket-Extensions"],
                Is.EqualTo("permessage-deflate; client_no_context_takeover; server_no_context_takeover"));
        }

        [Test]
        public async Task MismatchedExtensions()
        {
            httpRequest.Headers["Sec-WebSocket-Extensions"] = "unknown extension";
            var serverExtensions = new IServerExtension[] { new DeflateExtension() };
            wsHandshake = new WsHandshake(parser.Object, composer.Object, serverExtensions, null);
            var handShakeResult = await wsHandshake.Performhandshake(Stream.Null);

            composer.Verify(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null));
            Assert.That(handShakeResult.Stream, Is.EqualTo(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.SwitchingProtocols));
            Assert.That(spyedResponse.Headers.ContainsKey("Sec-WebSocket-Extensions"), Is.False);
        }

        [Test]
        public void InvalidExtensionList()
        {
            httpRequest.Headers["Sec-WebSocket-Extensions"] = ",";
            var serverExtensions = new IServerExtension[] { new DeflateExtension() };
            wsHandshake = new WsHandshake(parser.Object, composer.Object, serverExtensions, null);
            Assert.ThrowsAsync<HandshakeException>(() => wsHandshake.Performhandshake(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task NegotiationHook()
        {
            var hook =
                new Func<HttpRequest, HttpResponse, Dictionary<string, object>, Task<bool>>(
                    (req, rsp, env) =>
                    {
                        rsp.Status = HttpStatusCode.Forbidden;
                        rsp.Headers["CustomHeader"] = "CustomValue";
                        return Task.FromResult(false);
                    });

            wsHandshake = new WsHandshake(parser.Object, composer.Object, new IServerExtension[] { }, hook);
            var handShakeResult = await wsHandshake.Performhandshake(Stream.Null);

            composer.Verify(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null));
            Assert.That(handShakeResult.Stream, Is.EqualTo(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(spyedResponse.Headers.ContainsKey("Sec-WebSocket-Accept"), Is.False);
            Assert.That(spyedResponse.Headers["CustomHeader"], Is.EqualTo("CustomValue"));
        }

        private static HttpRequest prepareValidHttpRequest()
        {
            var httpRequest = new HttpRequest();
            httpRequest.Headers.Add("Host", "origin.org");
            httpRequest.Headers.Add("Upgrade", "websocket");
            httpRequest.Headers.Add("Connection", "upgrade");
            httpRequest.Headers.Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==");
            httpRequest.Headers.Add("Sec-WebSocket-Version", "13");
            return httpRequest;
        }
    }
}