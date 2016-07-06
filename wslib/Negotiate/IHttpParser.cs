using System.IO;
using System.Threading.Tasks;

namespace wslib.Negotiate
{
    public interface IHttpParser
    {
        Task<HttpRequest> ParseHttpRequest(Stream stream);
    }
}