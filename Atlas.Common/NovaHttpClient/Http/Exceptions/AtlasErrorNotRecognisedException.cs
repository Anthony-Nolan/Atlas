using System;
using System.Net;
using Atlas.Common.Utils.Http;

namespace Atlas.Common.NovaHttpClient.Http.Exceptions
{
    public class AtlasErrorNotRecognisedException : AtlasHttpException
    {
        private const string ErrorString = "Error format not recognised";

        public AtlasErrorNotRecognisedException() : base(HttpStatusCode.InternalServerError, ErrorString)
        {
        }

        public AtlasErrorNotRecognisedException(Exception innerException) : base(HttpStatusCode.NotFound, ErrorString, innerException)
        {
        }
    }
}
