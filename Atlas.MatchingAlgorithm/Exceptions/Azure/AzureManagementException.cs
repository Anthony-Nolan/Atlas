using System;

namespace Atlas.MatchingAlgorithm.Exceptions.Azure
{
    public class AzureManagementException : Exception
    {
        public AzureManagementException(string message) : base(message)
        {
        }
    }
}