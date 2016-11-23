using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using wslib.Utils;

namespace wslib.Protocol
{
    public static class WsDissector
    {
        public static async Task<WsFrame> ReadFrameHeader(Stream stream, ArraySegment<byte> receiveBuffer, bool expectMask, CancellationToken cancellationToken)
        {
            if (receiveBuffer.Count < 14)
                throw new ArgumentException("receive buffer is too small");

            var headerLength = expectMask ? 6 : 2;
            await stream.ReadUntil(receiveBuffer, 0, headerLength, cancellationToken).ConfigureAwait(false);

            bool actualMask = (receiveBuffer.At(1) > 127);
            if (expectMask ^ actualMask)
                throw new ProtocolViolationException("mask flag has wrong value");

            int maskOffset = 2;
            ulong payloadLength = (ulong)(receiveBuffer.At(1) & 0x7F);
            if (payloadLength > 125)
            {
                if (payloadLength == 126)
                {
                    await stream.ReadUntil(receiveBuffer, headerLength, headerLength + 2, cancellationToken).ConfigureAwait(false);
                    payloadLength = StreamExtensions.ReadN(receiveBuffer, 2, 2);
                    maskOffset = 4;
                }
                else if (payloadLength == 127)
                {
                    await stream.ReadUntil(receiveBuffer, headerLength, headerLength + 8, cancellationToken).ConfigureAwait(false);
                    payloadLength = StreamExtensions.ReadN(receiveBuffer, 2, 8);
                    maskOffset = 10;
                }
            }

            var header = new WsFrameHeader(receiveBuffer.At(0), receiveBuffer.At(1));
            var frame = new WsFrame(header, payloadLength, new ArraySegment<byte>(receiveBuffer.Array, receiveBuffer.Offset + maskOffset, 4));
            return frame;
        }

        public static ArraySegment<byte> SerializeFrameHeader(WsFrameHeader wsFrameHeader, int payloadLen, byte[] mask) // TODO: accept segment as in
        {
            int headerLen = 2;
            byte[] header = new byte[14];
            wsFrameHeader.CopyTo(new ArraySegment<byte>(header, 0, 2));

            if (wsFrameHeader.MASK)
                header[1] = 0x80;

            if (payloadLen <= 125)
            {
                header[1] |= (byte)payloadLen;
            }
            else if (payloadLen <= ushort.MaxValue)
            {
                headerLen += 2;
                header[1] |= 126;
                header[2] = (byte)(payloadLen >> 8);
                header[3] = (byte)(payloadLen & 0xff);
            }
            else
            {
                headerLen += 8;
                header[1] |= 127;
                int i = 0;
                while (payloadLen > 0)
                {
                    header[9 - i] = (byte)(payloadLen & 0xff);
                    payloadLen = payloadLen >> 8;
                    i++;
                }
            }

            if (wsFrameHeader.MASK)
            {
                if (mask.Length != 4) throw new ArgumentException("mask length isn't 4 bytes");
                Buffer.BlockCopy(mask, 0, header, headerLen, 4);
                headerLen += 4;
            }

            return new ArraySegment<byte>(header, 0, headerLen);
        }
    }
}