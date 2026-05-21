using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Validation;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models;
using FluentValidation;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IDonorBatchProcessor<TDonor, TResult>
    {
        /// <summary>
        /// Sequentially processes a batch of donors.
        /// </summary>
        /// <param name="donorInfo">Batch of donor info of type TDonor to be processed.</param>
        /// <param name="processDonorInfoFuncAsync">Function to be run on each donor info, that generates an object of type TResult.</param>
        /// <param name="getFailedDonorInfo">Function to select failed donor info.</param>
        /// <param name="failureEventName">Name to use when logging the processing failure event.</param>
        /// <returns>Results from processing of the donor batch.</returns>
        Task<DonorBatchProcessingResult<TResult>> ProcessBatchAsyncWithAnticipatedExceptions<TException>(
            IEnumerable<TDonor> donorInfo,
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<TDonor, FailedDonorInfo> getFailedDonorInfo,
            string failureEventName)
        where TException : Exception;
    }

    public abstract class DonorBatchProcessor<TDonor, TResult> : IDonorBatchProcessor<TDonor, TResult>
    {
        private readonly IMatchingAlgorithmImportLogger logger;

        protected DonorBatchProcessor(IMatchingAlgorithmImportLogger logger)
        {
            this.logger = logger;
        }

        public async Task<DonorBatchProcessingResult<TResult>> ProcessBatchAsyncWithAnticipatedExceptions<TException>(
            IEnumerable<TDonor> donorInfo,
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<TDonor, FailedDonorInfo> getFailedDonorInfo,
            string failureEventName)
            where TException : Exception
        {
            donorInfo = donorInfo.ToList();

            if (!donorInfo.Any())
            {
                return new DonorBatchProcessingResult<TResult>();
            }

            var results = new List<TResult>();
            var failedDonors = new List<FailedDonorInfo>();

            foreach (var d in donorInfo)
            {
                var result = await ProcessDonorInfoWithAnticipatedException<TException>(
                        processDonorInfoFuncAsync,
                        getFailedDonorInfo,
                        failureEventName,
                        d,
                        failedDonors);

                if (result != null)
                {
                    results.Add(result);
                }
            }

            return new DonorBatchProcessingResult<TResult>
            {
                ProcessingResults = results.AsReadOnly(),
                FailedDonors = failedDonors.AsReadOnly()
            };
        }

        private async Task<TResult> ProcessDonorInfoWithAnticipatedException<TException>(
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<TDonor, FailedDonorInfo> getFailedDonorInfo,
            string failureEventName,
            TDonor d,
            ICollection<FailedDonorInfo> failedDonors)
        where TException : Exception
        {
            try
            {
                return await processDonorInfoFuncAsync(d);
            }
            catch (TException e)
            {
                var failedDonorInfo = getFailedDonorInfo(d);
                failedDonors.Add(failedDonorInfo);

                var props = new Dictionary<string, string>
                {
                    { "Exception", e.ToString() },
                    { "AtlasDonorId", failedDonorInfo.AtlasDonorId.ToString() },
                    { "DonorInfo", JsonConvert.SerializeObject(failedDonorInfo.DonorInfo) }
                };

                switch (e)
                {
                    case HlaMetadataDictionaryException hlaEx:
                        props["Locus"] = hlaEx.Locus;
                        props["HlaName"] = hlaEx.HlaName;
                        props["InnerExceptionType"] = hlaEx.InnerException?.GetType().Name ?? "N/A";
                        break;
                    case ValidationException valEx:
                        props["ValidationErrors"] = valEx.ToErrorMessagesString();
                        break;
                    default:
                        props["ExceptionType"] = e.GetType().FullName;
                        break;
                }

                logger.SendEvent(failureEventName, LogLevel.Error, props);

                return default;
            }
        }
    }
}
