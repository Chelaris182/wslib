using System.IO;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using wslib.Models;
using wslib.Negotiate;

namespace UnitTests
{
    public class NegotiationTests
    {
        // TODO: add a test for negotiation timeout

        [Test]
        public async Task NoUpgradeHeader()
        {
            var parser = new Mock<IHttpParser>();
            parser.Setup(httpParser => httpParser.ParseHttpRequest(Stream.Null)).ReturnsAsync(new HttpRequest());

            var composer = new Mock<IHttpComposer>();
            var wsHandshake = new WsHandshake(null, parser.Object, composer.Object);
            await wsHandshake.Performhandshake(Stream.Null); // must return some HTTP code
        }

        [Test]
        public async Task Success()
        {
            var httpRequest = new HttpRequest();
            httpRequest.Headers.Add("Upgrade", "websocket");
            httpRequest.Headers.Add("Connection", "upgrade");
            httpRequest.Headers.Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==");
            httpRequest.Headers.Add("Sec-WebSocket-Version", "13");
            var parser = new Mock<IHttpParser>();
            parser.Setup(httpParser => httpParser.ParseHttpRequest(Stream.Null)).ReturnsAsync(httpRequest);

            HttpResponse spyedResponse = null;
            var composer = new Mock<IHttpComposer>();
            composer
                .Setup(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null))
                .Returns(() => Task.FromResult(true))
                .Callback<HttpResponse, Stream>((response, _) => { spyedResponse = response; });

            var wsHandshake = new WsHandshake(null, parser.Object, composer.Object);
            var handShakeResult = await wsHandshake.Performhandshake(Stream.Null);

            composer.Verify(httpComposer => httpComposer.WriteResponse(It.IsAny<HttpResponse>(), Stream.Null));
            Assert.That(handShakeResult.Stream, Is.EqualTo(Stream.Null));
            Assert.That(spyedResponse.Headers["Sec-WebSocket-Accept"], Is.EqualTo("s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));
        }
    }
}