using System;

namespace Atlas.DonorImport.Exceptions
{
    public class DuplicateDonorImportException : Exception
    {
        public DuplicateDonorImportException(string message): base(message)
        {
        }
    }
}