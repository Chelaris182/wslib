using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol.Writer
{
    public interface IWsMessageWriteStream : IDisposable
    {
        Task WriteFrame(WsFrameHeader wsFrameHeader, byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}