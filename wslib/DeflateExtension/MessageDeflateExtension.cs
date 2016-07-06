using System.IO;
using System.IO.Compression;
using wslib.Protocol;
using wslib.Protocol.Writer;

namespace wslib.DeflateExtension
{
    public class MessageDeflateExtension : IMessageExtension
    {
        public Stream ApplyRead(Stream payloadStream, WsFrame frame)
        {
            if (frame.Header.RSV1)
                return new DeflateStream(payloadStream, CompressionMode.Decompress, true);
            return payloadStream;
        }

        public IWsMessageWriteStream ApplyWrite(IWsMessageWriteStream stream)
        {
            stream = new WsSplitStream(stream);
            return WsDeflateWriteStream.Create(stream);
        }
    }
}