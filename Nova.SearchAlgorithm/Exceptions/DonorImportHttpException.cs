using System;
using System.Net;
using Nova.Utils.Http.Exceptions;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class DonorImportHttpException : NovaHttpException
    {
        public DonorImportHttpException(string message)
            : base(HttpStatusCode.InternalServerError, message)
        {
        }

        public DonorImportHttpException(string message, Exception inner)
            : base(HttpStatusCode.InternalServerError, message, inner)
        {
        }
    }
}