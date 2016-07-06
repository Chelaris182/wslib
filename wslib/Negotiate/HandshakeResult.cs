using System.Collections.Generic;
using System.IO;
using wslib.Protocol;

namespace wslib.Negotiate
{
    public class HandshakeResult
    {
        public readonly Dictionary<string, object> Env;
        public readonly List<IMessageExtension> Extensions;
        public readonly Stream Stream;

        public HandshakeResult(Dictionary<string, object> env, List<IMessageExtension> extensions, Stream stream)
        {
            Env = env;
            Extensions = extensions;
            Stream = stream;
        }
    }
}