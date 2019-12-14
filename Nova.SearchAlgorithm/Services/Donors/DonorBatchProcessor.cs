using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    public interface IDonorBatchProcessor<TDonor, TResult, TException>
        where TException : Exception
    {
        /// <param name="donorInfo">Batch of donor info of type TDonor to be processed.</param>
        /// <param name="processDonorInfoFuncAsync">Function to be run on each donor info, that generates an object of type TResult.</param>
        /// <param name="getEventModelFunc">Function to generate the processing failure model in the event that an exception of type TException is raised during processing."</param>
        /// <param name="getFailedDonorInfo">Function to select failed donor info.</param>
        /// <returns>Results from processing of the donor batch.</returns>
        Task<DonorBatchProcessingResult<TResult>> ProcessBatchAsync(
            IEnumerable<TDonor> donorInfo,
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<DonorProcessingException<TException>, DonorProcessingFailureEventModel> getEventModelFunc,
            Func<TDonor, FailedDonorInfo> getFailedDonorInfo);
    }

    public abstract class DonorBatchProcessor<TDonor, TResult, TException> : IDonorBatchProcessor<TDonor, TResult, TException>
        where TException : Exception
    {
        private readonly ILogger logger;

        protected DonorBatchProcessor(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<DonorBatchProcessingResult<TResult>> ProcessBatchAsync(
            IEnumerable<TDonor> donorInfo,
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<DonorProcessingException<TException>, DonorProcessingFailureEventModel> getEventModelFunc,
            Func<TDonor, FailedDonorInfo> getFailedDonorInfo)
        {
            donorInfo = donorInfo.ToList();
            if (!donorInfo.Any())
            {
                return new DonorBatchProcessingResult<TResult>();
            }

            var failedDonors = new List<FailedDonorInfo>();

            var results = await Task.WhenAll(donorInfo.Select(async d =>
                {
                    try
                    {
                        return await processDonorInfoFuncAsync(d);
                    }
                    catch (TException e)
                    {
                        var failedDonorInfo = getFailedDonorInfo(d);
                        failedDonors.Add(failedDonorInfo);
                        logger.SendEvent(getEventModelFunc(new DonorProcessingException<TException>(failedDonorInfo, e)));

                        return default;
                    }
                }
            ));

            return new DonorBatchProcessingResult<TResult>
            {
                ProcessingResults = results.Where(d => d != null),
                FailedDonors = failedDonors
            };
        }
    }
}
