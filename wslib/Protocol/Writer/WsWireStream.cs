using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol.Writer
{
    public class WsWireStream : IWsMessageWriteStream
    {
        private readonly bool cacheData;
        private readonly byte[] cache;
        private int cacheLength;

        public WsWireStream(bool cacheData, Stream innerStream) : base(innerStream, false)
        {
            this.cacheData = cacheData;
            if (cacheData)
                cache = new byte[81920];
        }

        public override Task WriteHeader(WsFrameHeader.Opcodes opcode, bool finFlag, bool rsv1, int payloadLen, CancellationToken cancellationToken)
        {
            var header = WsDissector.CreateFrameHeader(finFlag, rsv1, opcode, payloadLen);
            return WriteAsync(header.Array, header.Offset, header.Count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!cacheData)
            {
                await base.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                return;
            }

            while (count > 0)
            {
                int toCopy = Math.Min(cache.Length - cacheLength, count);
                Buffer.BlockCopy(buffer, offset, cache, cacheLength, toCopy);
                cacheLength += toCopy;
                count -= toCopy;
                if (cacheLength == cache.Length)
                    await flushCache(cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cacheLength > 0)
            {
                await flushCache(cancellationToken).ConfigureAwait(false);
            }

            await base.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task flushCache(CancellationToken cancellationToken)
        {
            var toWrite = cacheLength;
            cacheLength = 0;
            return base.WriteAsync(cache, 0, toWrite, cancellationToken);
        }
    }
}