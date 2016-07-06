using System.IO;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;

namespace wslib.Utils
{
    public static class StreamExtensions
    {
        public static async Task ReadUntil(this Stream stream, byte[] buffer, int have, int need, CancellationToken cancellationToken)
        {
            while (have < need)
            {
                var r = await stream.ReadAsync(buffer, have, need - have, cancellationToken).ConfigureAwait(false);
                if (r <= 0) throw new IOException(); // TODO: throw some valid exception
                have += r;
            }
        }

        public static ulong ReadN(byte[] buffer, int offset, int count)
        {
            ulong n = 0;
            for (var i = 0; i < count; i++)
            {
                n = n << 8;
                n += buffer[offset + i];
            }
            return n;
        }

        public static async Task<byte[]> ReadPayload(this WsMessage message)
        {
            var destination = new MemoryStream();
            await message.ReadStream.CopyToAsync(destination).ConfigureAwait(false);
            return destination.ToArray();
        }
    }
}