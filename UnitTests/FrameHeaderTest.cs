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
    }
}