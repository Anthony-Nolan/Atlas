using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class DonorManagementException : Exception
    {
        private const string ErrorMessage = "Error when managing donors.";

        public DonorManagementException(Exception inner) : base(ErrorMessage, inner)
        {
        }
    }
}
