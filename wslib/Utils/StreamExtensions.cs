using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;

namespace wslib.Utils
{
    public static class StreamExtensions
    {
        public static async Task ReadUntil(this Stream stream, ArraySegment<byte> buffer, int have, int need, CancellationToken cancellationToken)
        {
            if (buffer.Count < need)
                throw new ArgumentException("invalid read buffer");

            while (have < need)
            {
                var r = await stream.ReadAsync(buffer.Array, buffer.Offset + have, need - have, cancellationToken).ConfigureAwait(false);
                if (r <= 0) throw new IOException();
                have += r;
            }
        }

        public static ulong ReadN(ArraySegment<byte> buffer, int offset, int count)
        {
            ulong n = 0;
            for (var i = 0; i < count; i++)
            {
                n = n << 8;
                n += buffer.At(offset + i);
            }
            return n;
        }

        public static async Task<byte[]> ReadPayload(this WsMessage message)
        {
            var destination = new MemoryStream();
            await message.ReadStream.CopyToAsync(destination).ConfigureAwait(false);
            return destination.ToArray();
        }

        public static Task WriteAsync(this Stream stream, ArraySegment<byte> array, CancellationToken cancellationToken)
        {
            return stream.WriteAsync(array.Array, array.Offset, array.Count, cancellationToken);
        }

        public static T At<T>(this ArraySegment<T> buffer, int offset)
        {
            return buffer.Array[buffer.Offset + offset];
        }
    }
}