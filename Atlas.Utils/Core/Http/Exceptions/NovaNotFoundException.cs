using System;
using System.Net;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public class NovaNotFoundException : NovaHttpException
    {
        public NovaNotFoundException(string message) : base(HttpStatusCode.NotFound, message)
        {
        }

        public NovaNotFoundException(string message, Exception innerException)
            : base(HttpStatusCode.NotFound, message, innerException)
        {
        }
    }
}
