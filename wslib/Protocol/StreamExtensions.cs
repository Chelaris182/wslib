using System;
using System.IO;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    public static class StreamExtensions
    {
        public static async Task ReadUntil(this Stream stream, byte[] buffer, int have, int need)
        {
            while (have < need)
            {
                var r = await stream.ReadAsync(buffer, have, need - have).ConfigureAwait(false);
                if (r <= 0) throw new Exception("a"); // TODO: throw some valid exception
                have += r;
            }
        }

        public static ulong ReadN(byte[] buffer, int offset, int count)
        {
            ulong n = 0;
            for (var i = 0; i < count; i++) n = n << 8 + buffer[offset + i];
            return n;
        }
    }
}