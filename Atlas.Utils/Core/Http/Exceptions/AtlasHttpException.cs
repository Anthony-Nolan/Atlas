using System;
using System.Net;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public class AtlasHttpException : Exception
    {
        public AtlasHttpException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public AtlasHttpException(HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
