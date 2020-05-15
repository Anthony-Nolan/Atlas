using System;
using System.Net;
using Atlas.Common.Utils.Http;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class MatchingDictionaryHttpException : AtlasHttpException
    {
        public MatchingDictionaryHttpException(string message)
            : base(HttpStatusCode.InternalServerError, message)
        {
        }

        public MatchingDictionaryHttpException(string message, Exception inner)
            : base(HttpStatusCode.InternalServerError, message, inner)
        {
        }
    }
}