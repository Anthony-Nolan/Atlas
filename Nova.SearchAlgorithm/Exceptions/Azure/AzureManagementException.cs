using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class AzureManagementException : Exception
    {
        public AzureManagementException(string message) : base(message)
        {
        }
    }
}