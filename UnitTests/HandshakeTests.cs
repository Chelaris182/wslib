using System.IO;
using System.Net;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using wslib.Negotiate;
using wslib.Protocol;

namespace UnitTests
{
    public class HandshakeTests
    {
        // TODO: add a test for negotiation timeout

        // Request requirements https://tools.ietf.org/html/rfc6455#section-4.1
        [Test]
        [TestCase("Host")]
        [TestCase("Upgrade")]
        [TestCase("Connection")]
        [TestCase("Sec-WebSocket-Key")]
        [TestCase("Sec-WebSocket-Version")]
        public void MissingHeader(string header)
        {
            var httpRequest = prepareValidHttpRequest();
            httpRequest.Headers.Remove(header);
            var parser = new Mock<IHttpParser>();
            parser.Setup(httpParser => httpParser.ParseHttpRequest(Stream.Null)).ReturnsAsync(httpRequest);

            HttpResponse spyedResponse = null;
            var composer = new Mock<IHttpComposer>();
            composer
                .Setup(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null))
                .Returns(() => Task.FromResult(true))
                .Callback<HttpResponse, Stream>((response, _) => { spyedResponse = response; });

            var wsHandshake = new WsHandshake(parser.Object, composer.Object, new IServerExtension[] { }, null);
            Assert.ThrowsAsync<HandshakeException>(() => wsHandshake.Performhandshake(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task Success()
        {
            var httpRequest = prepareValidHttpRequest();
            var parser = new Mock<IHttpParser>();
            parser.Setup(httpParser => httpParser.ParseHttpRequest(Stream.Null)).ReturnsAsync(httpRequest);

            HttpResponse spyedResponse = null;
            var composer = new Mock<IHttpComposer>();
            composer
                .Setup(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null))
                .Returns(() => Task.FromResult(true))
                .Callback<HttpResponse, Stream>((response, _) => { spyedResponse = response; });

            var wsHandshake = new WsHandshake(parser.Object, composer.Object, new IServerExtension[] { }, null);
            var handShakeResult = await wsHandshake.Performhandshake(Stream.Null);

            composer.Verify(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null));
            Assert.That(handShakeResult.Stream, Is.EqualTo(Stream.Null));
            Assert.That(spyedResponse.Status, Is.EqualTo(HttpStatusCode.SwitchingProtocols));
            Assert.That(spyedResponse.Headers["Upgrade"], Is.EqualTo("websocket"));
            Assert.That(spyedResponse.Headers["Connection"], Is.EqualTo("Upgrade"));
            Assert.That(spyedResponse.Headers["Sec-WebSocket-Accept"], Is.EqualTo("s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));
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