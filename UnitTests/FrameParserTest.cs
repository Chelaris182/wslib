using NUnit.Framework;
using wslib.Protocol;

namespace UnitTests
{
    /// <summary>
    /// Summary description for FrameParserTest
    /// </summary>
    public class FrameParserTest
    {
        [Test]
        public void Test()
        {
            var frame = new WsFrameHeader(129, 101);
            Assert.That(frame.FIN, Is.True);
            Assert.That(frame.OPCODE, Is.EqualTo(WsFrameHeader.Opcodes.TEXT));
            Assert.That(frame.MASK, Is.False);
        }
    }
}