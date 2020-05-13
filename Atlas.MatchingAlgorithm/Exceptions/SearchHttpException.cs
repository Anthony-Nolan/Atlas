using System;
using System.Net;
using Atlas.Utils.Core.Http.Exceptions;

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