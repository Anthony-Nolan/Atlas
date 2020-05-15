using System.Net;
using Atlas.Utils.Core.Http;

namespace Atlas.Utils.NovaHttpClient.Http.Exceptions
{
    public class AtlasNotFoundException : AtlasHttpException
    {
        public AtlasNotFoundException(string message) : base(HttpStatusCode.NotFound, message)
        {
        }
    }
}
