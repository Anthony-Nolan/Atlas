using System;

namespace Atlas.DonorImport.Exceptions
{
    internal class DonorFormatException : Exception
    {
        private const string ErrorMessage = "Error parsing Donor Format";
        public DonorFormatException(Exception e): base(ErrorMessage, e)
        {
        }
    }
}