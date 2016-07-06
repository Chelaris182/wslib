using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using wslib.Negotiate;

namespace UnitTests
{
    class HttpComposerTest
    {
        [Test]
        public async Task HttpComposeTest()
        {
            var httpComposer = new HttpComposer();
            var stream = new MemoryStream();

            await httpComposer.WriteResponse(new HttpResponse(), stream);
            var response = Encoding.ASCII.GetString(stream.ToArray());
            Assert.That(response, Is.EqualTo("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n\r\n"));
        }
    }
}