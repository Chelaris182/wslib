using wslib.Negotiate.Extensions;
using wslib.Protocol;

namespace wslib.DeflateExtension
{
    public class DeflateExtension : IServerExtension
    {
        public bool TryMatch(string token, ExtensionParams extensionParams, out ExtensionParams matchedParams, out IMessageExtension messageExtension)
        {
            matchedParams = null;
            messageExtension = null;
            if (!string.Equals(token, "permessage-deflate")) return false;
            matchedParams = new ExtensionParams();
            matchedParams.Add("client_no_context_takeover");
            matchedParams.Add("server_no_context_takeover");

            messageExtension = new MessageDeflateExtension();
            return true;
        }
    }
}