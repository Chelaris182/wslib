using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;
using wslib.Protocol.Writer;

namespace wslib.DeflateExtension
{
    class WsSplitStream : IWsMessageWriteStream
    {
        private WsFrameHeader.Opcodes frameOpcode;
        private bool frameFinFlag;
        private bool frameRsv1;
        private bool flushed = true;

        public WsSplitStream(IWsMessageWriteStream innerStream) : base(innerStream, false)
        {
        }

        public override async Task WriteHeader(WsFrameHeader.Opcodes opcode, bool finFlag, bool rsv1, int payloadLen, CancellationToken cancellationToken)
        {
            if (!flushed)
            {
                await FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            frameOpcode = opcode;
            frameFinFlag = finFlag;
            frameRsv1 = rsv1;
            flushed = false;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await ((IWsMessageWriteStream)InnerStream).WriteHeader(frameOpcode, false, frameRsv1, count, cancellationToken).ConfigureAwait(false);
            await InnerStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            frameOpcode = WsFrameHeader.Opcodes.CONTINUATION;
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            flushed = true;
            if (frameFinFlag == false) return;
            await ((IWsMessageWriteStream)InnerStream).WriteHeader(WsFrameHeader.Opcodes.CONTINUATION, frameFinFlag, false, 0, cancellationToken).ConfigureAwait(false);
            await base.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}