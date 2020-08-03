using System;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.ExternalInterface.Models;

namespace Atlas.DonorImport.Exceptions
{
    public class DuplicateDonorImportException : Exception
    {
        public DuplicateDonorImportException(string message): base(message)
        {
        }
    }
}