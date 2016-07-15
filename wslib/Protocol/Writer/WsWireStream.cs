using System.IO;
using System.Threading;
using System.Threading.Tasks;
using wslib.Utils;

namespace wslib.Protocol.Writer
{
    public class WsWireStream : StreamWrapper, IWsMessageWriteStream
    {
        public WsWireStream(Stream innerStream) : base(innerStream, false)
        {
        }

        public async Task WriteFrame(WsFrameHeader wsFrameHeader, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var header = WsDissector.SerializeFrameHeader(wsFrameHeader, count, null);
            await WriteAsync(header.Array, header.Offset, header.Count, cancellationToken).ConfigureAwait(false);
            await WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }
    }
}