using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace wslib.Negotiate.Extensions
{
    static class HandshakeExtensions
    {
        public static IEnumerable<ExtensionRequest> ParseExtensionHeader(string value)
        {
            var extensions = value.Split(new[] { ',' }, 2); // limited number of extensions
            var extensionList = new List<ExtensionRequest>();
            foreach (var extension in extensions)
            {
                var parameters = new ExtensionParams();
                if (string.IsNullOrEmpty(extension)) throw new HandshakeException("empty extension-token");
                var extensionParams = extension.Split(';');
                foreach (var param in extensionParams.Skip(1).Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var tokens = param.Split(new[] { '=' }, 2);
                    parameters.Add(tokens[0], tokens.Length == 2 ? tokens[1] : string.Empty);
                }

                extensionList.Add(new ExtensionRequest(extensionParams[0], parameters));
            }

            return extensionList;
        }

        public static string ComposeExtensionHeader(List<ExtensionRequest> matchedExtensions)
        {
            var sb = new StringBuilder();
            foreach (var extension in matchedExtensions)
            {
                sb.Append(extension.Token);
                foreach (var p in extension.Params)
                {
                    sb.Append("; ");
                    sb.Append(p.Item1);
                    if (string.IsNullOrEmpty(p.Item2)) continue;
                    sb.Append("=");
                    sb.Append(p.Item2);
                }
            }

            return sb.ToString();
        }
    }
}