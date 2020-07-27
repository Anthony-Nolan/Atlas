using System;

namespace Atlas.DonorImport.Exceptions
{
    internal class EmptyDonorFileException : Exception
    {
        private const string ErrorMessage = "Donor file did not have any contents";
        public EmptyDonorFileException() : base(ErrorMessage)
        {
        }
    }
}