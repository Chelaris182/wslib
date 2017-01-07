using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests.Utils;
using wslib.DeflateExtension;
using wslib.Protocol;

namespace UnitTests
{
    public class DeflateTests
    {
        [Test]
        public async Task TestRead()
        {
            byte[] payload = { 0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00 };
            using (var ms = new MemoryStream(payload))
            {
                var extensions = new List<IMessageExtension> { new MessageDeflateExtension() };
                using (var websocket = new WebSocket(null, ms, extensions, false))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes("Hello")));
                }
            }
        }

        [Test]
        public async Task TestFragmentedRead()
        {
            byte[] payload = { 0x41, 0x03, 0xf2, 0x48, 0xcd, 0x80, 0x04, 0xc9, 0xc9, 0x07, 0x00 };
            using (var ms = new MemoryStream(payload))
            {
                var extensions = new List<IMessageExtension> { new MessageDeflateExtension() };
                using (var websocket = new WebSocket(null, ms, extensions, false))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes("Hello")));
                }
            }
        }

        [Test]
        public async Task TestReadDeflateWitoutCompression()
        {
            byte[] payload = { 0xc1, 0x0b, 0x00, 0x05, 0x00, 0xfa, 0xff, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x00 };
            using (var ms = new MemoryStream(payload))
            {
                var extensions = new List<IMessageExtension> { new MessageDeflateExtension() };
                using (var websocket = new WebSocket(null, ms, extensions, false))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes("Hello")));
                }
            }
        }

        [Test]
        public async Task TestReadDeflateWithBFINAL()
        {
            byte[] payload = { 0xc1, 0x09, 0xf3, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00, 0x00 };
            using (var ms = new MemoryStream(payload))
            {
                var extensions = new List<IMessageExtension> { new MessageDeflateExtension() };
                using (var websocket = new WebSocket(null, ms, extensions, false))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes("Hello")));
                }
            }
        }

        [Test]
        public async Task TestReadTwoDeflateBlocks()
        {
            byte[] payload = { 0xc1, 0x0c, 0xf2, 0x48, 0x05, 0x00, 0x00, 0x00, 0xff, 0xff, 0xca, 0xc9, 0xc9, 0x07, 0x00 };
            using (var ms = new MemoryStream(payload))
            {
                var extensions = new List<IMessageExtension> { new MessageDeflateExtension() };
                using (var websocket = new WebSocket(null, ms, extensions, false))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes("Hello")));
                }
            }
        }

        [Test]
        public async Task TestReadWriteHello()
        {
            byte[] rawText = Encoding.UTF8.GetBytes(RandomGeneration.RandomString(1, 129));
            using (var ms = new MemoryStream())
            {
                var extensions = new List<IMessageExtension> { new MessageDeflateExtension() };
                using (var websocket = new WebSocket(null, ms, extensions, false))
                {
                    using (var writer = await websocket.CreateMessageWriter(MessageType.Text, CancellationToken.None))
                    {
                        await writer.WriteMessageAsync(rawText, 0, rawText.Length, CancellationToken.None);
                    }

                    ms.Position = 0;
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(rawText));
                }
            }
        }
    }
}