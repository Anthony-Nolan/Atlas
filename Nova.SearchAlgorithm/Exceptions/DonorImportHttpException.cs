using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class DonorImportHttpException : Exception
    {
        public DonorImportHttpException(string message)
            : base(message)
        {
        }

        public DonorImportHttpException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}