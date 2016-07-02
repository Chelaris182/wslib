using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using wslib.Models;

namespace wslib.Negotiate
{
    class HttpParser : IHttpParser
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
            var tokens = line.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 2) throw new HandshakeException("Invalid header");
            httpRequest.Headers[tokens[0].TrimEnd()] = tokens[1].TrimStart();
        }

        private void parseRequestLine(string requestLine, HttpRequest httpRequest)
        {
            var tokens = requestLine.Split(new[] { ' ' }, 3);
            if (tokens.Length != 3) throw new HandshakeException("Invalid request line");
            if (tokens[0] != "GET") throw new HandshakeException("Unsupported method: " + tokens[0]);
            httpRequest.RequestUri = new Uri(tokens[1], UriKind.Relative);
        }
    }
}