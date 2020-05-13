using System;
using System.Net;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public class AtlasNotFoundException : AtlasHttpException
    {
        public AtlasNotFoundException(string message) : base(HttpStatusCode.NotFound, message)
        {
        }

        public AtlasNotFoundException(string message, Exception innerException)
            : base(HttpStatusCode.NotFound, message, innerException)
        {
        }
    }
}
