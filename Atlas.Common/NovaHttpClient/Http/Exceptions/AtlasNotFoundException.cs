using System.Net;
using Atlas.Common.Utils.Http;

namespace Atlas.Common.NovaHttpClient.Http.Exceptions
{
    public class AtlasNotFoundException : AtlasHttpException
    {
        public AtlasNotFoundException(string message) : base(HttpStatusCode.NotFound, message)
        {
        }
    }
}
