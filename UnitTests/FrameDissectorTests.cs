using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using wslib.Protocol;

namespace UnitTests
{
    public class FrameDissectorTests
    {
        [Test]
        [TestCase(new byte[] { 0x91, 0x83, 0x00, 0x00, 0x00, 0x00 }, (ulong)0x03)]
        [TestCase(new byte[] { 0x91, 0xFD, 0x00, 0x00, 0x00, 0x00 }, (ulong)0x7D)]
        [TestCase(new byte[] { 0x91, 0xFE, 0x12, 0x34, 0x00, 0x00, 0x00, 0x00 }, (ulong)0x1234)]
        [TestCase(new byte[] { 0x91, 0xFF, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x00, 0x00, 0x00, 0x00 }, (ulong)0x1234567812345678)]
        public async Task TestLength(byte[] message, ulong length)
        {
            var stream = new MemoryStream();
            stream.Write(message, 0, message.Length);
            stream.Position = 0;
            var frame = await WsDissector.ReadFrameHeader(stream);
            Assert.That(frame.Header.FIN, Is.True);
            Assert.That(frame.PayloadLength, Is.EqualTo(length));
        }

        [Test]
        [TestCase(new byte[] { 0x91, 0x83, 0x00, 0x00 })]
        public void TestInsufficientData(byte[] message)
        {
            var stream = new MemoryStream();
            stream.Write(message, 0, message.Length);
            stream.Position = 0;
            Assert.ThrowsAsync<IOException>(() => WsDissector.ReadFrameHeader(stream));
        }
    }
}