using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests.Utils;
using wslib.Protocol;
using wslib.Protocol.Writer;
using wslib.Utils;

namespace UnitTests
{
    /// <summary>
    /// Summary description for WriteStreamTest
    /// </summary>
    public class WriteStreamTest
    {
        [Test]
        public async Task TestWriteRead()
        {
            using (var source = new MemoryStream())
            {
                var randomString = RandomGeneration.RandomString(1, 65536 * 4);
                var payload1 = Encoding.UTF8.GetBytes(randomString);

                randomString = RandomGeneration.RandomString(1, 65536 * 4);
                var payload2 = Encoding.UTF8.GetBytes(randomString);
                using (var writer = new WsMessageWriter(MessageType.Text, () => { }, new WsWireStream(source)))
                {
                    await writer.WriteMessageAsync(payload1, 0, payload1.Length, CancellationToken.None);
                    await writer.WriteMessageAsync(payload2, 0, payload2.Length, CancellationToken.None);
                }

                source.Seek(0, SeekOrigin.Begin);
                using (var webSocket = new WebSocket(null, source, null, false))
                {
                    Assert.That(webSocket.IsConnected, Is.True);

                    using (var message = await webSocket.ReadMessageAsync(CancellationToken.None))
                    {
                        Assert.That(message, Is.Not.Null);
                        Assert.That(message.Type, Is.EqualTo(MessageType.Text));

                        var result = await message.ReadPayload();
                        Assert.That(result, Is.EqualTo(payload1));
                        Assert.That(webSocket.IsConnected, Is.True);
                    }

                    using (var message = await webSocket.ReadMessageAsync(CancellationToken.None))
                    {
                        Assert.That(message, Is.Not.Null);
                        Assert.That(message.Type, Is.EqualTo(MessageType.Text));

                        var result = await message.ReadPayload();
                        Assert.That(result, Is.EqualTo(payload2));
                        Assert.That(webSocket.IsConnected, Is.True);
                    }

                    using (var message = await webSocket.ReadMessageAsync(CancellationToken.None))
                    {
                        Assert.That(message, Is.Null);
                    }
                }
            }
        }
    }
}