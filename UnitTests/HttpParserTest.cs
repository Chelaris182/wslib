using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using wslib.Negotiate;

namespace UnitTests
{
    class HttpParserTest
    {
        [Test]
        public async Task ParseRequestTest()
        {
            var r = @"GET /ws HTTP/1.1
Host: 127.0.0.1:8080
Connection: Upgrade
Pragma: no-cache
Cache-Control: no-cache
Upgrade: websocket
Origin: chrome-extension://dmogdjmcpfaibncngoolgljgocdabhke
Sec-WebSocket-Version: 13
User-Agent: Mozilla/5.0(Windows NT 10.0; WOW64) AppleWebKit/537.36(KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36
Accept-Encoding: gzip, deflate, sdch
Accept-Language: en,en-US; q=0.8,ru; q=0.6
Sec-WebSocket-Key: eaS3ZrZc8+QbHFGbtsOUNQ==
Sec-WebSocket-Extensions: permessage-deflate; client_max_window_bits

";

            var bytes = Encoding.ASCII.GetBytes(r);
            var stream = new MemoryStream(bytes);

            var httpParser = new HttpParser();
            var request = await httpParser.ParseHttpRequest(stream);
            Assert.That(request.Headers["Sec-WebSocket-Key"], Is.EqualTo("eaS3ZrZc8+QbHFGbtsOUNQ=="));
        }

        [Test]
        public void ParseEmptyStreamTest()
        {
            var httpParser = new HttpParser();
            var stream = new MemoryStream();
            Assert.ThrowsAsync<HandshakeException>(() => httpParser.ParseHttpRequest(stream));
        }

        [Test]
        [TestCase("GET")]
        [TestCase("GET /ws")]
        [TestCase("GET /ws  ")]
        [TestCase("POST /ws HTTP/1.1")]
        [TestCase("GET ws HTTP/1.1")]
        [TestCase("GET /ws HTTP/1.1\r\nField")]
        public void ParseInvalidRequestTest(string r)
        {
            var bytes = Encoding.ASCII.GetBytes(r);
            var stream = new MemoryStream(bytes);
            var httpParser = new HttpParser();
            Assert.ThrowsAsync<HandshakeException>(() => httpParser.ParseHttpRequest(stream));
        }
    }
}