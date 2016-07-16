using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using wslib.Protocol;

namespace UnitTests
{
    public class FrameDissectorTests
    {
        [Test]
        [TestCase(new byte[] { 0x91, 0x83, 0x98, 0x76, 0x54, 0x32 }, (ulong)0x03)]
        [TestCase(new byte[] { 0x91, 0xFD, 0x98, 0x76, 0x54, 0x32 }, (ulong)0x7D)]
        [TestCase(new byte[] { 0x91, 0xFE, 0x12, 0x34, 0x98, 0x76, 0x54, 0x32 }, (ulong)0x1234)]
        [TestCase(new byte[] { 0x91, 0xFF, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x98, 0x76, 0x54, 0x32 }, (ulong)0x1234567812345678)]
        public async Task TestLengthDecodingMasked(byte[] message, ulong length)
        {
            var receiveBuffer = new ArraySegment<byte>(new byte[100]);
            var stream = new MemoryStream(message);
            var frame = await WsDissector.ReadFrameHeader(stream, receiveBuffer, true, CancellationToken.None);
            Assert.That(frame.Header.FIN, Is.True);
            Assert.That(frame.Header.MASK, Is.True);
            Assert.That(frame.PayloadLength, Is.EqualTo(length));
            Assert.That(frame.Mask, Is.EqualTo(new byte[] { 0x98, 0x76, 0x54, 0x32 }));
        }

        [Test]
        [TestCase(new byte[] { 0x91, 0x03 }, (ulong)0x03)]
        [TestCase(new byte[] { 0x91, 0x7D }, (ulong)0x7D)]
        [TestCase(new byte[] { 0x91, 0x7E, 0x12, 0x34 }, (ulong)0x1234)]
        [TestCase(new byte[] { 0x91, 0x7F, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78 }, (ulong)0x1234567812345678)]
        public async Task TestLengthDecodingWithoutMask(byte[] message, ulong length)
        {
            var receiveBuffer = new ArraySegment<byte>(new byte[100]);
            var stream = new MemoryStream(message);
            var frame = await WsDissector.ReadFrameHeader(stream, receiveBuffer, false, CancellationToken.None);
            Assert.That(frame.Header.FIN, Is.True);
            Assert.That(frame.Header.MASK, Is.False);
            Assert.That(frame.PayloadLength, Is.EqualTo(length));
        }

        [Test]
        [TestCase(new byte[] { 0x91, 0x83, 0x00, 0x00 })]
        public void TestInsufficientData(byte[] message)
        {
            var receiveBuffer = new ArraySegment<byte>(new byte[100]);
            var stream = new MemoryStream(message);
            Assert.ThrowsAsync<IOException>(() => WsDissector.ReadFrameHeader(stream, receiveBuffer, true, CancellationToken.None));
        }
    }
}