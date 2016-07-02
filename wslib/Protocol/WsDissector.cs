using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    class WsDissector
    {
        public static async Task<WsFrame> ReadFrameHeader(Stream stream)
        {
            var buf = new byte[14];
            var headerLength = 6;
            await stream.ReadUntil(buf, 0, headerLength).ConfigureAwait(false);

            bool masked = (buf[1] > 127);
            if (!masked) // all frames sent from the client to the server are masked
                throw new ProtocolViolationException();

            int maskOffset = 2;
            ulong payloadLength = (ulong)(buf[1] & 0x7F);
            if (payloadLength > 125)
            {
                if (payloadLength == 126)
                {
                    headerLength += 2;
                    maskOffset += 2;
                    await stream.ReadUntil(buf, 6, headerLength).ConfigureAwait(false);
                    payloadLength = StreamExtensions.ReadN(buf, 2, 2);
                }
                else if (payloadLength == 127)
                {
                    headerLength += 8;
                    maskOffset += 8;
                    await stream.ReadUntil(buf, 6, headerLength).ConfigureAwait(false);
                    payloadLength = StreamExtensions.ReadN(buf, 2, 8);
                }
            }

            byte[] mask = new byte[4];
            Buffer.BlockCopy(buf, maskOffset, mask, 0, 4);

            var header = new WsFrameHeader(buf[0], buf[1]);
            var frame = new WsFrame(header, payloadLength, mask);
            return frame;
        }

        public static ArraySegment<byte> CreateFrameHeader(bool finFlag, WsFrameHeader.Opcodes opcode, int payloadLen)
        {
            int headerLen = 2;
            byte[] header = new byte[10];
            header[0] = (byte)(finFlag ? 0x80 : 0);
            header[0] |= (byte)opcode;
            if (payloadLen <= 125)
            {
                header[1] = (byte)payloadLen;
            }
            else if (payloadLen < ushort.MaxValue)
            {
                headerLen += 2;
                header[1] = 126;
                header[2] = (byte)(payloadLen >> 8);
                header[3] = (byte)(payloadLen & 0xff);
            }
            else
            {
                headerLen += 8;
                header[1] = 127;
                throw new NotImplementedException(); // TODO: fix serialization
            }
            return new ArraySegment<byte>(header, 0, headerLen);
        }
    }
}