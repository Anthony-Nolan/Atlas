using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class ManageDonorFunctionException : Exception
    {
        private const string ErrorMessage = "Error when running Manage Donor function.";

        public ManageDonorFunctionException(Exception inner) : base(ErrorMessage, inner)
        {
        }
    }
}
