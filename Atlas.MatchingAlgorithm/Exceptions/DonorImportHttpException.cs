using System;
using System.Net;
using Atlas.Utils.Core.Http;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    public class DonorImportHttpException : AtlasHttpException
    {
        public DonorImportHttpException(string message, Exception inner) : base(HttpStatusCode.InternalServerError, message, inner)
        {
        }
    }
}