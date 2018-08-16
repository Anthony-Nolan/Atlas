using System;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions
{
    public class DonorNotFoundException : Exception
    {
        public DonorNotFoundException(string message) : base(message)
        {
        }
    }
}