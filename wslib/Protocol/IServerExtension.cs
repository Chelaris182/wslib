using wslib.Negotiate.Extensions;

namespace wslib.Protocol
{
    public interface IServerExtension
    {
        bool TryMatch(string token, ExtensionParams extensionParams, out ExtensionParams matchedParams, out IMessageExtension messageExtension);
    }
}