using System.IO;
using System.Linq;
using System.Text;
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
                ms.Write(payload, 0, payload.Length);
                ms.Position = 0;
                var header = new WsFrameHeader(0x80, 0x80);
                var frame = new WsFrame(header, (ulong)payload.Length, new byte[] { 0, 0, 0, 0 });
                using (var stream = new WsReadStream(frame, ms, false))
                {
                    var destination = new MemoryStream();
                    await stream.CopyToAsync(destination);
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
                for (int i = 0; i < payload.Length; i++)
                    ms.WriteByte((byte)(payload[i] ^ mask[i % 4]));
                ms.Position = 0;
                var header = new WsFrameHeader(0x80, 0x80);
                var frame = new WsFrame(header, (ulong)payload.Length, mask);
                using (var stream = new WsReadStream(frame, ms, false))
                {
                    var destination = new MemoryStream();
                    await stream.CopyToAsync(destination);
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
                for (int i = 0; i < payload1.Length; i++)
                    ms.WriteByte((byte)(payload1[i] ^ mask1[i % 4]));
                ms.WriteByte(0x80); // second header
                ms.WriteByte((byte)(0x80 | payload2.Length));
                ms.Write(mask2, 0, mask2.Length);
                for (int i = 0; i < payload2.Length; i++)
                    ms.WriteByte((byte)(payload2[i] ^ mask2[i % 4]));

                ms.Position = 0;
                var header = new WsFrameHeader(0x00, 0x80);
                var frame = new WsFrame(header, (ulong)payload1.Length, mask1);
                using (var stream = new WsReadStream(frame, ms, false))
                {
                    var destination = new MemoryStream();
                    await stream.CopyToAsync(destination);
                    var r = destination.ToArray();
                    Assert.That(r.Take(payload1.Length), Is.EqualTo(payload1));
                    Assert.That(r.Skip(payload1.Length), Is.EqualTo(payload2));
                }
            }
        }
    }
}