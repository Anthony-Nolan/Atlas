using System;

namespace Atlas.DonorImport.Exceptions
{
    public class DuplicateDonorException : Exception
    {
        public DuplicateDonorException(string message) : base(message)
        {
        }
    }
}
