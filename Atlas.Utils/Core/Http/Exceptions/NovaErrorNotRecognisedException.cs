using System;
using System.Net;

namespace Atlas.Utils.Core.Http.Exceptions
{
    public class NovaErrorNotRecognisedException : NovaHttpException
    {
        private const string ErrorString = "Error format not recognised";

        public NovaErrorNotRecognisedException() : base(HttpStatusCode.InternalServerError, ErrorString)
        {
        }

        public NovaErrorNotRecognisedException(Exception innerException)
            : base(HttpStatusCode.NotFound, ErrorString, innerException)
        {
        }
    }
}
