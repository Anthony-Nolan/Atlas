using System;

namespace Atlas.DonorImport.Exceptions
{
    internal class MalformedDonorFileException : Exception
    {
        internal MalformedDonorFileException(string message) : base(message)
        {
        }
    }
}