using System;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions
{
    public class InvalidTestDataException : Exception
    {
        public InvalidTestDataException(string message) : base(message)
        {
        }
    }
}