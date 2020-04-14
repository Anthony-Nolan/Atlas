using System;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    public class AzureManagementException : Exception
    {
        public AzureManagementException(string message) : base(message)
        {
        }
    }
}