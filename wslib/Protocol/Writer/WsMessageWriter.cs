using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol.Writer
{
    public class WsMessageWriter : IDisposable
    {
        private readonly MessageType messageType;
        private readonly Action onDispose;
        private readonly IWsMessageWriteStream stream;

        public WsMessageWriter(MessageType messageType, Action onDispose, IWsMessageWriteStream stream)
        {
            this.messageType = messageType;
            this.onDispose = onDispose;
            this.stream = stream;
        }

        public Task WriteFrame(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var header = generateFrameHeader(false); // TODO: change opcode to continuation
            return stream.WriteFrame(header, buffer, offset, count, cancellationToken);
        }

        public Task CloseMessageAsync(CancellationToken cancellationToken)
        {
            var header = generateFrameHeader(true);
            return stream.WriteFrame(header, new byte[0], 0, 0, cancellationToken);
        }

        public Task WriteMessageAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var header = generateFrameHeader(true);
            return stream.WriteFrame(header, buffer, offset, count, cancellationToken);
        }

        private WsFrameHeader generateFrameHeader(bool finFlag)
        {
            var opcode = messageType == MessageType.Text ? WsFrameHeader.Opcodes.TEXT : WsFrameHeader.Opcodes.BINARY;
            return new WsFrameHeader { FIN = finFlag, OPCODE = opcode };
        }

        public void Dispose()
        {
            stream.Dispose();
            onDispose();
        }
    }
}