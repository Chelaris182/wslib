using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;
using wslib.Protocol.Writer;
using wslib.Utils;

namespace wslib.DeflateExtension
{
    class WsDeflateWriteStream : WsWriterWrapper
    {
        private readonly MemoryStream proxy = new MemoryStream(128);

        public WsDeflateWriteStream(IWsMessageWriteStream innerStream) : base(innerStream, false)
        {
        }

        public override Task WriteFrame(WsFrameHeader wsFrameHeader, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count > 0)
            {
                using (DeflateStream deflateStream = new DeflateStream(proxy, CompressionMode.Compress, true))
                {
                    deflateStream.Write(buffer, offset, count);
                } // deflateStream flushes internal data on dispose only

                buffer = proxy.GetBuffer();
                count = (int)proxy.Length;
                wsFrameHeader.RSV1 = true;
            }

            return base.WriteFrame(wsFrameHeader, buffer, 0, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                proxy.Dispose();
            base.Dispose(disposing);
        }
    }
}