using System;

namespace wslib.Protocol
{
    public struct WsFrame
    {
        public readonly WsFrameHeader Header;
        public readonly ulong PayloadLength;
        public readonly ArraySegment<byte> Mask;

        public WsFrame(WsFrameHeader header, ulong payloadLength, ArraySegment<byte> mask)
        {
            Header = header;
            PayloadLength = payloadLength;
            Mask = mask;
        }
    }
}