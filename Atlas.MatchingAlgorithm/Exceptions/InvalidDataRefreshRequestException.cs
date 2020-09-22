using System;
using System.Net;
using Atlas.Common.Utils.Http;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    internal class InvalidDataRefreshRequestHttpException : AtlasHttpException
    {
        public InvalidDataRefreshRequestHttpException(string message) : base(HttpStatusCode.BadRequest, message)
        {
        }

        public InvalidDataRefreshRequestHttpException(string message, Exception innerException) : base(HttpStatusCode.BadRequest, message, innerException)
        {
        }
    }
}
