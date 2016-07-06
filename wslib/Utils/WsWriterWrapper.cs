using System;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;
using wslib.Protocol.Writer;

namespace wslib.Utils
{
    class WsWriterWrapper : IWsMessageWriteStream
    {
        private readonly IWsMessageWriteStream innerStream;
        private readonly bool closeInnerStream;

        public WsWriterWrapper(IWsMessageWriteStream innerStream, bool closeInnerStream)
        {
            this.innerStream = innerStream;
            this.closeInnerStream = closeInnerStream;
        }

        public virtual Task WriteFrame(WsFrameHeader wsFrameHeader, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return innerStream.WriteFrame(wsFrameHeader, buffer, offset, count, cancellationToken);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (closeInnerStream) innerStream.Dispose();
            }
        }
    }
}