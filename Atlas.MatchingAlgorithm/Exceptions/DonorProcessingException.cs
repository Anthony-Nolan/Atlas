using Atlas.MatchingAlgorithm.Models;
using System;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    public class DonorProcessingException<TException> : Exception where TException : Exception
    {
        public FailedDonorInfo FailedDonorInfo { get; set; }
        public TException Exception { get; set; }

        public DonorProcessingException(FailedDonorInfo failedDonorInfo, TException exception)
        {
            FailedDonorInfo = failedDonorInfo;
            Exception = exception;
        }
    }
}