using System.IO;
using System.Threading.Tasks;

namespace wslib.Extensions
{
    interface IConnectionExtension
    {
        Task<Stream> ExtendConnectionAsync(Stream stream);
    }
}