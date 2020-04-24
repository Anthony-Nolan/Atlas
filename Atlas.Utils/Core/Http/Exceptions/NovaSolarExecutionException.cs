using System;
using System.Net;

namespace Nova.Utils.Http.Exceptions
{
    public class NovaSolarExecutionException : NovaHttpException
    {
        private const string FailureMessage = "The execution of a Solar procedure failed";

        public NovaSolarExecutionException() : base(HttpStatusCode.BadGateway, FailureMessage)
        {
        }

        public NovaSolarExecutionException(Exception innerException) : base(HttpStatusCode.BadGateway, FailureMessage, innerException)
        {
        }
    }
}
