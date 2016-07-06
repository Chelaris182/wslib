using System;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;
using wslib.Protocol.Writer;

namespace wslib
{
    public interface IWebSocket : IDisposable
    {
        Task<WsMessage> ReadMessageAsync(CancellationToken cancellationToken);
        Task<WsMessageWriter> CreateMessageWriter(MessageType type, CancellationToken cancellationToken);
        bool IsConnected();
    }
}