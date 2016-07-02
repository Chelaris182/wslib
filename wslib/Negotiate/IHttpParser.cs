using System.IO;
using System.Threading.Tasks;
using wslib.Models;

namespace wslib.Negotiate
{
    public interface IHttpParser
    {
        Task<HttpRequest> ParseHttpRequest(Stream stream);
    }
}