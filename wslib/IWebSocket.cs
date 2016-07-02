using System;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;

namespace wslib
{
    public interface IWebSocket : IDisposable
    {
        Task<WsMessage> ReadMessageAsync(CancellationToken cancellationToken);
        Task<WsMessageWriter> CreateWriter(MessageType type, CancellationToken cancellationToken);
        bool IsConnected();
    }
}