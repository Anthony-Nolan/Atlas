using System;
using System.Net;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public class NovaAuthenticationException : NovaHttpException
    {
        // .Net calls 401 'Unauthorized' instead of unauthenticated, because reasons.
        public NovaAuthenticationException() : base(HttpStatusCode.Unauthorized, "Request is not authenticated.")
        {
        }

        public NovaAuthenticationException(string message, Exception innerException)
            : base(HttpStatusCode.Unauthorized, message, innerException)
        {
        }
    }
}
