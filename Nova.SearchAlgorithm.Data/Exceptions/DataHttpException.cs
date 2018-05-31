using System;
using System.Net;
using Nova.Utils.Http.Exceptions;

namespace Nova.SearchAlgorithm.Data.Exceptions
{
    /// <summary>
    /// Exception for issues which occur in the Nova.SearchAlgorithm.Data project.
    /// </summary>
    public class DataHttpException : NovaHttpException
    {
        public DataHttpException(string message)
            : base(HttpStatusCode.InternalServerError, message)
        {
        }

        public DataHttpException(string message, Exception inner)
            : base(HttpStatusCode.InternalServerError, message, inner)
        {
        }
    }
}