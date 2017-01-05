using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace wslib.Negotiate
{
    public class HttpComposer : IHttpComposer
    {
        public async Task WriteResponse(HttpResponse httpResponse, Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII, 1024, true))
            {
                if (httpResponse.Status == HttpStatusCode.SwitchingProtocols)
                {
                    writer.Write("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n");
                }
                else
                {
                    writeStatusLine(writer, httpResponse.Status);
                }

                writeHeaders(writer, httpResponse);
                writeCrlf(writer);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        private static void writeCrlf(StreamWriter writer)
        {
            writer.Write("\r\n");
        }

        private void writeHeaders(StreamWriter writer, HttpResponse httpResponse)
        {
            foreach (var header in httpResponse.Headers)
            {
                writer.Write(header.Key);
                writer.Write(": ");
                writer.Write(header.Value);
                writer.Write("\r\n");
            }
        }

        private void writeStatusLine(StreamWriter writer, HttpStatusCode status)
        {
            writer.Write("HTTP/1.1 ");
            writer.Write((int)status);
            writer.Write(" ");
            writer.Write(" "); // FIXME: HttpWorkerRequest.GetStatusDescription((int)status));
        }
    }
}