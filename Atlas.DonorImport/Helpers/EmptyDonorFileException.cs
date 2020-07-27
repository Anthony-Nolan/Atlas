using System;

namespace Atlas.DonorImport.Helpers
{
    internal class EmptyDonorFileException : Exception
    {
        private const string ErrorMessage = "Donor file did not have any contents";
        public EmptyDonorFileException() : base(ErrorMessage)
        {
        }
    }
}