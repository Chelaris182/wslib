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
        public WsDeflateWriteStream(IWsMessageWriteStream innerStream) : base(innerStream, false)
        {
        }

        public override Task WriteFrame(WsFrameHeader wsFrameHeader, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var proxy = new MemoryStream();
            using (DeflateStream deflateStream = new DeflateStream(proxy, CompressionMode.Compress))
            {
                deflateStream.Write(buffer, offset, count);
            } // deflateStream flushes internal data on dispose only

            wsFrameHeader.RSV1 = true;
            var deflatedData = proxy.ToArray();
            return base.WriteFrame(wsFrameHeader, deflatedData, 0, deflatedData.Length, cancellationToken);
        }
    }
}