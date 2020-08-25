using System;

namespace Atlas.DonorImport.Exceptions
{
    public class DonorNotFoundException : Exception
    {
        public DonorNotFoundException(string message) : base(message)
        {
        }
    }
}