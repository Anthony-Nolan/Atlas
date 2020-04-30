using System;
using System.Net;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public class NovaHttpException : Exception
    {
        public NovaHttpException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public NovaHttpException(HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
