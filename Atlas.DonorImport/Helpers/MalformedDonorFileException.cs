using System;

namespace Atlas.DonorImport.Helpers
{
    internal class MalformedDonorFileException : Exception
    {
        internal MalformedDonorFileException(string message) : base(message)
        {
        }
    }
}