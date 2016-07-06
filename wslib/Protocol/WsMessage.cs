using System;
using System.IO;

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
        public readonly Stream ReadStream;

        public WsMessage(MessageType type, Stream stream)
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