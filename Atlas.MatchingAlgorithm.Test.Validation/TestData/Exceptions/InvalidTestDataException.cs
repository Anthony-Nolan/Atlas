using System;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions
{
    public class InvalidTestDataException : Exception
    {
        public InvalidTestDataException(string message) : base(message)
        {
        }
    }
}