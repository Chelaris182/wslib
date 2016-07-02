using System.IO;
using System.Threading.Tasks;

namespace wslib.Negotiate
{
    public interface IHttpComposer
    {
        Task WriteResponse(HttpResponse httpResponse, Stream stream);
    }
}