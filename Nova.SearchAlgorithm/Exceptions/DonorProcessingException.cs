using Nova.SearchAlgorithm.Models;
using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class DonorProcessingException<T> : Exception where T : Exception
    {
        public FailedDonorInfo FailedDonorInfo { get; set; }
        public T Exception { get; set; }

        public DonorProcessingException(FailedDonorInfo failedDonorInfo, T exception)
        {
            FailedDonorInfo = failedDonorInfo;
            Exception = exception;
        }
    }
}