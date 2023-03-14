using System;

namespace Atlas.Common.AzureStorage.Blob
{
    public class BlobNotFoundException : Exception
    {
        public BlobNotFoundException(string container, string fileName) : base(message: $"Could not find blob at location: {container}/{fileName}")
        {
        }
    }
}