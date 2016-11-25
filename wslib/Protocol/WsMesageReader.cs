using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    public class WsMesageReader
    {
        private readonly WebSocket ws;
        private WsFrame currentFrame;
        private ulong framePayloadLen;
        private ulong framePosition;

        public WsMesageReader(WebSocket ws, WsFrame frame)
        {
            this.ws = ws;
            currentFrame = frame;
            framePayloadLen = frame.PayloadLength;
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count == 0) return 0;

            int r = 0;
            do
            {
                if (framePayloadLen > 0)
                {
                    int toRead = (int)Math.Min(framePayloadLen, (ulong)count);
                    r = await ws.ReadAsync(buffer, offset, toRead, cancellationToken).ConfigureAwait(false);
                    if (r <= 0) return r;

                    if (currentFrame.Header.MASK)
                        inplaceUnmask(buffer, offset, r);

                    framePayloadLen -= (ulong)r;
                    framePosition += (ulong)r;
                }

                if (framePayloadLen == 0)
                {
                    // if this is the last frame of the message, return
                    if (currentFrame.Header.FIN) return r;

                    // read the next frame
                    currentFrame = await ws.ReadFrameAsync(cancellationToken).ConfigureAwait(false);
                    if (currentFrame.Header.OPCODE != WsFrameHeader.Opcodes.CONTINUATION)
                    {
                        await ws.CloseAsync(CloseStatusCode.ProtocolError, cancellationToken).ConfigureAwait(false);
                        throw new ProtocolViolationException("Fragmented message was aborted");
                    }

                    framePayloadLen = currentFrame.PayloadLength;
                    framePosition = 0;
                }
            } while (r == 0); // spin until we find a non-empty frame or EOM

            return r;
        }

        private unsafe void inplaceUnmask(byte[] buffer, int offset, int count)
        {
            var maskIndex = (int)(framePosition & 3);

            fixed (byte* pMask = &currentFrame.Mask.Array[currentFrame.Mask.Offset])
            fixed (byte* pBuffer = buffer)
            {
                byte* p = pBuffer + offset;
                byte* end = p + count;

                while (p < end)
                {
                    if (maskIndex == 0)
                    {
                        uint maski = *(uint*)pMask;
                        ulong maskl = (ulong)maski << 32 | maski;
                        while (p + 8 < end)
                        {
                            *(ulong*)p ^= maskl;
                            p += 8;
                        }

                        while (p + 4 < end)
                        {
                            *(uint*)p ^= maski;
                            p += 4;
                        }

                        if (p >= end) break;
                    }

                    *p++ ^= pMask[maskIndex];
                    maskIndex = (maskIndex + 1) & 3;
                }
            }
        }
    }
}