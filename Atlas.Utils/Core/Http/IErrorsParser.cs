using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Atlas.Utils.Core.Http
{
    public interface IErrorsParser
    {
        Task<bool> ThrowBadRequestException(HttpContent content);
        Task ThrowGenericException(HttpStatusCode statusCode, HttpContent content);
        Task ThrowNotFoundException(HttpContent content);
    }
}