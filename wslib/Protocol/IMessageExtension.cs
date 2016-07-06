using System.IO;
using wslib.Protocol.Writer;

namespace wslib.Protocol
{
    public interface IMessageExtension
    {
        Stream ApplyRead(Stream payloadStream, WsFrame frame);

        IWsMessageWriteStream ApplyWrite(IWsMessageWriteStream stream);
    }
}