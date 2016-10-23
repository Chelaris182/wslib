using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using wslib;
using wslib.Protocol;

namespace UnitTests
{
    class HeartbitTests
    {
        private static readonly TimeSpan pingPeriod = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan inactivityPeriod = TimeSpan.FromMilliseconds(500);

        [Test]
        public async Task TestDisconnectedSocket()
        {
            var ws = new Mock<IWebSocket>(MockBehavior.Strict);
            ws.Setup(socket => socket.IsConnected()).Returns(false);
            await Heartbit.RunHeartbit(ws.Object, pingPeriod, inactivityPeriod);
        }

        [Test]
        public async Task TestDisconnectingSocket()
        {
            var ws = new Mock<IWebSocket>(MockBehavior.Strict);
            ws.Setup(socket => socket.LastActivity()).Returns(() => DateTime.Now);
            ws.SetupSequence(socket => socket.IsConnected())
              .Returns(true)
              .Returns(true)
              .Returns(false);
            await Heartbit.RunHeartbit(ws.Object, pingPeriod, inactivityPeriod);
        }

        [Test]
        public async Task TestAliveSocket()
        {
            var ws = new Mock<IWebSocket>(MockBehavior.Strict);
            ws.Setup(socket => socket.SendMessageAsync(
                  It.Is<WsFrameHeader.Opcodes>(opcodes => opcodes == WsFrameHeader.Opcodes.PING),
                  It.IsAny<ArraySegment<byte>>(),
                  It.IsAny<CancellationToken>()))
              .Returns(Task.FromResult(true));
            ws.Setup(socket => socket.IsConnected()).Returns(true);
            ws.Setup(socket => socket.CloseAsync(It.IsAny<CloseStatusCode>(), It.IsAny<CancellationToken>()))
              .Returns(Task.FromResult(true));
            ws.Setup(socket => socket.LastActivity()).Returns(DateTime.Now);

            await Heartbit.RunHeartbit(ws.Object, pingPeriod, inactivityPeriod);

            ws.Verify(socket => socket.SendMessageAsync(
                It.Is<WsFrameHeader.Opcodes>(opcodes => opcodes == WsFrameHeader.Opcodes.PING),
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            ws.Verify(socket => socket.CloseAsync(It.IsAny<CloseStatusCode>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}