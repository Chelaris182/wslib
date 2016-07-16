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
            int toRead = (int)Math.Min(framePayloadLen, (ulong)count);
            if (toRead == 0) return 0;

            var r = await ws.ReadAsync(buffer, offset, toRead, cancellationToken).ConfigureAwait(false);
            if (r <= 0) return r;

            if (currentFrame.Header.MASK)
                inplaceUnmask(buffer, offset, r);

            framePayloadLen -= (ulong)r;
            framePosition += (ulong)r;
            if (framePayloadLen == 0 && !currentFrame.Header.FIN)
            {
                currentFrame = await ws.ReadFrameAsync(cancellationToken).ConfigureAwait(false);
                if (currentFrame.Header.OPCODE != WsFrameHeader.Opcodes.CONTINUATION)
                {
                    await ws.CloseAsync(CloseStatusCode.ProtocolError, cancellationToken).ConfigureAwait(false);
                    throw new ProtocolViolationException("Fragmented message was aborted");
                }

                framePayloadLen = currentFrame.PayloadLength;
                framePosition = 0;
            }

            return r;
        }

        private void inplaceUnmask(byte[] buffer, int offset, int count)
        {
            for (var i = offset; i < offset + count; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ currentFrame.Mask.At((int)(framePosition % 4)));
                framePosition++;
            }
        }
    }
}