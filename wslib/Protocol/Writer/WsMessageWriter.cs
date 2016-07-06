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

        public async Task WriteFrameAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await writeFrameHeader(false, count, cancellationToken).ConfigureAwait(false); // TODO: fix continuation opcode
            await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        public async Task CloseMessageAsync(CancellationToken cancellationToken)
        {
            await writeFrameHeader(true, 0, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteMessageAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await writeFrameHeader(true, count, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task writeFrameHeader(bool finFlag, int payloadLen, CancellationToken cancellationToken)
        {
            var opcode = messageType == MessageType.Text ? WsFrameHeader.Opcodes.TEXT : WsFrameHeader.Opcodes.BINARY;
            return stream.WriteHeader(opcode, finFlag, false, payloadLen, cancellationToken);
        }

        public void Dispose()
        {
            onDispose();
        }
    }
}