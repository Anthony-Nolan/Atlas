using System;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions
{
    public class MetaDonorNotFoundException : Exception
    {
        public MetaDonorNotFoundException(string message) : base(message)
        {
        }
    }
}