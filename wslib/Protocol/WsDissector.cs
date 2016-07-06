using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using wslib.Utils;

namespace wslib.Protocol
{
    public static class WsDissector
    {
        public static async Task<WsFrame> ReadFrameHeader(Stream stream, bool expectMask)
        {
            var buf = new byte[14];
            var headerLength = expectMask ? 6 : 2;
            await stream.ReadUntil(buf, 0, headerLength).ConfigureAwait(false);

            bool actualMask = (buf[1] > 127);
            if (expectMask ^ actualMask)
                throw new ProtocolViolationException("mask flag has wrong value");

            int maskOffset = 2;
            ulong payloadLength = (ulong)(buf[1] & 0x7F);
            if (payloadLength > 125)
            {
                if (payloadLength == 126)
                {
                    await stream.ReadUntil(buf, headerLength, headerLength + 2).ConfigureAwait(false);
                    payloadLength = StreamExtensions.ReadN(buf, 2, 2);
                    maskOffset = 4;
                }
                else if (payloadLength == 127)
                {
                    await stream.ReadUntil(buf, headerLength, headerLength + 8).ConfigureAwait(false);
                    payloadLength = StreamExtensions.ReadN(buf, 2, 8);
                    maskOffset = 10;
                }
            }

            byte[] mask = null; // TODO: replace with array segment
            if (actualMask)
            {
                mask = new byte[4];
                Buffer.BlockCopy(buf, maskOffset, mask, 0, 4);
            }

            var header = new WsFrameHeader(buf[0], buf[1]);
            var frame = new WsFrame(header, payloadLength, mask);
            return frame;
        }

        public static ArraySegment<byte> SerializeFrameHeader(WsFrameHeader wsFrameHeader, int payloadLen)
        {
            int headerLen = 2;
            byte[] header = new byte[10];
            wsFrameHeader.CopyTo(new ArraySegment<byte>(header, 0, 2));

            if (payloadLen <= 125)
            {
                header[1] |= (byte)payloadLen;
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