using System;
using System.Net;
using Atlas.Common.Utils.Http;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    public class DonorImportHttpException : AtlasHttpException
    {
        public DonorImportHttpException(string message, Exception inner) : base(HttpStatusCode.InternalServerError, message, inner)
        {
        }
    }
}