using System;

namespace Atlas.DonorImport.Exceptions
{
    public class DuplicateDonorFileImportException : Exception
    {
        public DuplicateDonorFileImportException(string fileName, string fileState) 
            : base($"Duplicate Donor File Import Attempt. File: {fileName} was started but already had an entry of state: {fileState}")
        {
        }
    }
}