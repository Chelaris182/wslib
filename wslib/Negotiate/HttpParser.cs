using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace wslib.Negotiate
{
    public class HttpParser : IHttpParser
    {
        public async Task<HttpRequest> ParseHttpRequest(Stream stream)
        {
            // TODO: wrap the stream with limiting wrapper to cope with long write attacks
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
            {
                string line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null) throw new HandshakeException("no request line");
                var httpRequest = new HttpRequest();
                parseRequestLine(line, httpRequest);

                line = await reader.ReadLineAsync().ConfigureAwait(false);
                while (!string.IsNullOrWhiteSpace(line))
                {
                    parseRequestHeader(line, httpRequest);
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                }
                return httpRequest;
            }
        }

        private void parseRequestHeader(string line, HttpRequest httpRequest)
        {
            var tokens = line.Split(new[] { ':' }, 2);
            if (tokens.Length != 2) throw new HandshakeException("Invalid header");
            httpRequest.Headers[tokens[0].TrimEnd()] = tokens[1].TrimStart(); // TODO: multi-line headers?
        }

        private void parseRequestLine(string requestLine, HttpRequest httpRequest)
        {
            var tokens = requestLine.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 3) throw new HandshakeException("Invalid request line");
            if (tokens[0] != "GET") throw new HandshakeException("Unsupported method: " + tokens[0]);
            if (!tokens[1].StartsWith("/")) throw new HandshakeException("Uri must start with /");
            httpRequest.RequestUri = new Uri(tokens[1], UriKind.Relative);
        }
    }
}