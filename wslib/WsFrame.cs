using wslib.Protocol;

namespace wslib
{
    public class WsFrame
    {
        public readonly WsFrameHeader Header;
        public readonly ulong PayloadLength;
        public readonly byte[] Mask;

        public WsFrame(WsFrameHeader header, ulong payloadLength, byte[] mask)
        {
            Header = header;
            PayloadLength = payloadLength;
            Mask = mask;
        }
    }
}