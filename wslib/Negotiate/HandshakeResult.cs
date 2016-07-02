using System.Collections.Generic;
using System.IO;

namespace wslib.Negotiate
{
    public class HandshakeResult
    {
        public readonly Dictionary<string, object> Env;
        public readonly Stream Stream;

        public HandshakeResult(Dictionary<string, object> env, Stream stream)
        {
            this.Env = env;
            this.Stream = stream;
        }
    }
}