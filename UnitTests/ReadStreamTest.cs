using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests.Utils;
using wslib.Protocol;

namespace UnitTests
{
    /// <summary>
    /// Summary description for ReadStreamTest
    /// </summary>
    public class ReadStreamTest
    {
        [Test, Repeat(10)]
        public async Task TestReadWithoutMask()
        {
            var payload = Encoding.UTF8.GetBytes(RandomGeneration.RandomString(1, 4096));
            using (var ms = new MemoryStream())
            {
                var header = new WsFrameHeader(0, 0) { FIN = true, OPCODE = WsFrameHeader.Opcodes.BINARY };
                var serializeFrameHeader = WsDissector.SerializeFrameHeader(header, payload.Length, null);
                ms.Write(serializeFrameHeader.Array, serializeFrameHeader.Offset, serializeFrameHeader.Count);
                ms.Write(payload, 0, payload.Length);
                ms.Position = 0;

                using (var websocket = new WebSocket(null, ms, null, false))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(payload));
                }
            }
        }

        [Test, Repeat(10)]
        public async Task TestReadWithMask()
        {
            var mask = RandomGeneration.RandomArray(4);
            var payload = Encoding.UTF8.GetBytes(RandomGeneration.RandomString(1, 4096));
            using (var ms = new MemoryStream())
            {
                var header = new WsFrameHeader(0, 0) { FIN = true, MASK = true, OPCODE = WsFrameHeader.Opcodes.BINARY };
                var serializeFrameHeader = WsDissector.SerializeFrameHeader(header, payload.Length, mask);
                ms.Write(serializeFrameHeader.Array, serializeFrameHeader.Offset, serializeFrameHeader.Count);
                for (int i = 0; i < payload.Length; i++)
                    ms.WriteByte((byte)(payload[i] ^ mask[i % 4]));
                ms.Position = 0;

                using (var websocket = new WebSocket(null, ms, null, true))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    Assert.That(destination.ToArray(), Is.EqualTo(payload));
                }
            }
        }

        [Test, Repeat(10)]
        public async Task TestMultiFrameRead()
        {
            var mask1 = RandomGeneration.RandomArray(4);
            var mask2 = RandomGeneration.RandomArray(4);
            var payload1 = Encoding.UTF8.GetBytes(RandomGeneration.RandomString(1, 100));
            var payload2 = Encoding.UTF8.GetBytes(RandomGeneration.RandomString(1, 100));
            using (var ms = new MemoryStream())
            {
                var header = new WsFrameHeader(0, 0) { FIN = false, MASK = true, OPCODE = WsFrameHeader.Opcodes.BINARY };
                var serializeFrameHeader = WsDissector.SerializeFrameHeader(header, payload1.Length, mask1);
                ms.Write(serializeFrameHeader.Array, serializeFrameHeader.Offset, serializeFrameHeader.Count);
                for (int i = 0; i < payload1.Length; i++)
                    ms.WriteByte((byte)(payload1[i] ^ mask1[i % 4]));

                header = new WsFrameHeader(0, 0) { FIN = true, MASK = true, OPCODE = WsFrameHeader.Opcodes.CONTINUATION };
                serializeFrameHeader = WsDissector.SerializeFrameHeader(header, payload2.Length, mask2);
                ms.Write(serializeFrameHeader.Array, serializeFrameHeader.Offset, serializeFrameHeader.Count);
                for (int i = 0; i < payload2.Length; i++)
                    ms.WriteByte((byte)(payload2[i] ^ mask2[i % 4]));

                ms.Position = 0;
                using (var websocket = new WebSocket(null, ms, null, true))
                {
                    var message = await websocket.ReadMessageAsync(CancellationToken.None);
                    Assert.That(message, Is.Not.Null);
                    var destination = new MemoryStream();
                    await message.ReadStream.CopyToAsync(destination);
                    var r = destination.ToArray();
                    Assert.That(r.Take(payload1.Length), Is.EqualTo(payload1));
                    Assert.That(r.Skip(payload1.Length), Is.EqualTo(payload2));
                }
            }
        }
    }
}