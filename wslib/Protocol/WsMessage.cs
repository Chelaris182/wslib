using System;

namespace wslib.Protocol
{
    public enum MessageType
    {
        Text,
        Binary
    }

    public class WsMessage : IDisposable
    {
        public readonly MessageType Type;
        public readonly WsReadStream ReadStream; // TODO: expose payload length

        public WsMessage(MessageType type, WsReadStream stream)
        {
            Type = type;
            ReadStream = stream;
        }

        public void Dispose()
        {
            ReadStream.Dispose();
        }
    }
}