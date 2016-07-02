using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    public class WsMessageWriter : StreamWrapper
    {
        private readonly MessageType messageType;
        private readonly Action onDispose;

        public WsMessageWriter(MessageType messageType, Action onDispose, Stream stream) : base(stream, false)
        {
            this.messageType = messageType;
            this.onDispose = onDispose;
        }

        public override void Close()
        {
            onDispose();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await writeFrameHeader(false, count, cancellationToken).ConfigureAwait(false);
            await base.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        private Task writeFrameHeader(bool finFlag, int payloadLen, CancellationToken cancellationToken)
        {
            var opcode = messageType == MessageType.Text ? WsFrameHeader.Opcodes.TEXT : WsFrameHeader.Opcodes.BINARY;
            var header = WsDissector.CreateFrameHeader(finFlag, opcode, payloadLen);
            return base.WriteAsync(header.Array, header.Offset, header.Count, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return writeFrameHeader(true, 0, cancellationToken);
        }

        public async Task WriteFinalAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await writeFrameHeader(true, count, cancellationToken).ConfigureAwait(false);
            await base.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        public override bool CanRead => false;
    }
}