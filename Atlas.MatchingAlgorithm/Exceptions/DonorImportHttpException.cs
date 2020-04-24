using System;
using System.Net;
using Atlas.Utils.Core.Http.Exceptions;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    public class DonorImportHttpException : AtlasHttpException
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