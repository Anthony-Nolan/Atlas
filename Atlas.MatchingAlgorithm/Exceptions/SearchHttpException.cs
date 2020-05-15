using System;
using System.Net;
using Atlas.Common.Utils.Http;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    public class SearchHttpException : AtlasHttpException
    {
        public SearchHttpException(string message)
            : base(HttpStatusCode.InternalServerError, message)
        {
        }

        public SearchHttpException(string message, Exception inner)
            : base(HttpStatusCode.InternalServerError, message, inner)
        {
        }
    }
}