using System;
using System.Net;
using Atlas.Utils.Core.Http;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class HlaMetadataDictionaryHttpException : AtlasHttpException
    {
        public HlaMetadataDictionaryHttpException(string message)
            : base(HttpStatusCode.InternalServerError, message)
        {
        }

        public HlaMetadataDictionaryHttpException(string message, Exception inner)
            : base(HttpStatusCode.InternalServerError, message, inner)
        {
        }
    }
}