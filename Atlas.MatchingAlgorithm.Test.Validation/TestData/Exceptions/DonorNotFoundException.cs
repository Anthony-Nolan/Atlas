using System;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions
{
    public class DonorNotFoundException : Exception
    {
        public DonorNotFoundException(string message) : base(message)
        {
        }
    }
}