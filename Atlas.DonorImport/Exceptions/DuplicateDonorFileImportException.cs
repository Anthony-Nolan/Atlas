using System;

namespace Atlas.DonorImport.Exceptions
{
    public class DuplicateDonorFileImportException : Exception
    {
        public DuplicateDonorFileImportException(string message): base(message)
        {
        }
    }
}