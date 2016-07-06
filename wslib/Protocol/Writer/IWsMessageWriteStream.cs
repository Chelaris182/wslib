using System.IO;
using System.Threading;
using System.Threading.Tasks;
using wslib.Utils;

namespace wslib.Protocol.Writer
{
    public abstract class IWsMessageWriteStream : StreamWrapper
    {
        public IWsMessageWriteStream(Stream innerStream, bool closeInnerStream) : base(innerStream, closeInnerStream)
        {
        }

        public abstract Task WriteHeader(WsFrameHeader.Opcodes opcode, bool finFlag, bool rsv1, int payloadLen, CancellationToken cancellationToken);
    }
}