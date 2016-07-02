﻿using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    public class WsReadStream : StreamWrapper
    {
        private WsFrame currentFrame;
        private ulong framePayloadLen;
        private ulong position;

        public WsReadStream(WsFrame frame, Stream innerStream, bool closeInnerStream) : base(innerStream, closeInnerStream)
        {
            currentFrame = frame;
            framePayloadLen = frame.PayloadLength;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int toRead = (int)Math.Min(framePayloadLen, (ulong)count);
            if (toRead == 0) return 0;

            var r = await base.ReadAsync(buffer, offset, toRead, cancellationToken).ConfigureAwait(false);
            if (r <= 0) return r;

            unmask(buffer, offset, r);

            framePayloadLen -= (ulong)r;
            position += (ulong)r;
            if (framePayloadLen == 0 && !currentFrame.Header.FIN)
            {
                currentFrame = await WsDissector.ReadFrameHeader(InnerStream).ConfigureAwait(false); // TODO: close connection gracefully
                if (currentFrame.Header.OPCODE != WsFrameHeader.Opcodes.CONTINUATION) throw new ProtocolViolationException();
                framePayloadLen = currentFrame.PayloadLength;
            }

            return r;
        }

        private void unmask(byte[] buffer, int offset, int count)
        {
            for (var i = offset; i < offset + count; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ currentFrame.Mask[position % 4]);
                position++;
            }
        }

        public override bool CanWrite => false;
    }
}