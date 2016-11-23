using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using wslib.Utils;

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

        private void inplaceUnmask(byte[] buffer, int offset, int count)
        {
            var p = framePosition;
            for (var i = offset; i < offset + count; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ currentFrame.Mask.At((int)(p % 4)));
                p++;
            }
        }
    }
}