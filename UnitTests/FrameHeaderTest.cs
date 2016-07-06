using System.Threading.Tasks;
using NUnit.Framework;
using wslib.Protocol;

namespace UnitTests
{
    public class FrameHeaderTest
    {
        [Test]
        public void BasicTest()
        {
            var frame = new WsFrameHeader(0x91, 0x7F);
            Assert.That(frame.FIN, Is.True);
            Assert.That(frame.RSV1, Is.False);
            Assert.That(frame.RSV2, Is.False);
            Assert.That(frame.RSV3, Is.True);
            Assert.That(frame.OPCODE, Is.EqualTo(WsFrameHeader.Opcodes.TEXT));
            Assert.That(frame.MASK, Is.False);
        }

        [Test]
        public void BasicTest2()
        {
            var frame = new WsFrameHeader(0x1A, 0xFF);
            Assert.That(frame.FIN, Is.False);
            Assert.That(frame.OPCODE, Is.EqualTo(WsFrameHeader.Opcodes.PONG));
            Assert.That(frame.MASK, Is.True);
        }

        [Test]
        [TestCase(new byte[] { 0x00, 0x00 }, false, false, WsFrameHeader.Opcodes.CONTINUATION)]
        [TestCase(new byte[] { 0x80, 0x00 }, true, false, WsFrameHeader.Opcodes.CONTINUATION)]
        [TestCase(new byte[] { 0xC0, 0x00 }, true, true, WsFrameHeader.Opcodes.CONTINUATION)]
        [TestCase(new byte[] { 0xC1, 0x00 }, true, true, WsFrameHeader.Opcodes.TEXT)]
        [TestCase(new byte[] { 0x42, 0x00 }, false, true, WsFrameHeader.Opcodes.BINARY)]
        public async Task TestBits(byte[] message, bool fin, bool rsv1, WsFrameHeader.Opcodes opcode)
        {
            var frame = new WsFrameHeader(message[0], message[1]);
            Assert.That(frame.FIN, Is.EqualTo(fin));
            Assert.That(frame.RSV1, Is.EqualTo(rsv1));
            Assert.That(frame.OPCODE, Is.EqualTo(opcode));
        }
    }
}