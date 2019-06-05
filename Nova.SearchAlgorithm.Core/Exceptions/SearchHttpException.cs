using System;
using System.Net;
using Nova.Utils.Http.Exceptions;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class SearchHttpException : NovaHttpException
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