using System;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions
{
    public class MetaDonorNotFoundException : Exception
    {
        public MetaDonorNotFoundException(string message) : base(message)
        {
        }
    }
}