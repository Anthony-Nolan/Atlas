using System;
using System.Net;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public class NovaSolarException : NovaHttpException
    {
        private const string FailureMessage = "The connection to SOLAR failed.";

        public NovaSolarException() : base(HttpStatusCode.BadGateway, FailureMessage)
        {
        }

        public NovaSolarException(Exception innerException) : base(HttpStatusCode.BadGateway, FailureMessage, innerException)
        {
        }
    }
}
