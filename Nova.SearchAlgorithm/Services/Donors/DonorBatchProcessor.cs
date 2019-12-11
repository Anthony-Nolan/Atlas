using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
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
        Task<IEnumerable<TResult>> ProcessBatchAsync(
            IEnumerable<TDonor> donorInfo,
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<DonorProcessingException<TException>, DonorProcessingFailureEventModel> getEventModelFunc,
            Func<TDonor, FailedDonorInfo> getFailedDonorInfo);
    }

    public abstract class DonorBatchProcessor<TDonor, TResult, TException> : IDonorBatchProcessor<TDonor, TResult, TException>
        where TException : Exception
    {
        private const string MissingDonorIdText = "[Donor(s) without ID]";

        private readonly ILogger logger;
        private readonly INotificationsClient notificationsClient;
        private readonly Priority loggerPriority;
        private readonly string alertSummary;

        protected DonorBatchProcessor(
            ILogger logger,
            INotificationsClient notificationsClient,
            Priority loggerPriority,
            string alertSummary)
        {
            this.logger = logger;
            this.notificationsClient = notificationsClient;
            this.loggerPriority = loggerPriority;
            this.alertSummary = alertSummary;
        }

        public async Task<IEnumerable<TResult>> ProcessBatchAsync(
            IEnumerable<TDonor> donorInfo,
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<DonorProcessingException<TException>, DonorProcessingFailureEventModel> getEventModelFunc,
            Func<TDonor, FailedDonorInfo> getFailedDonorInfo)
        {
            donorInfo = donorInfo.ToList();
            if (!donorInfo.Any())
            {
                return new List<TResult>();
            }

            var failedDonorIds = new List<string>();

            try
            {
                var results = await Task.WhenAll(donorInfo.Select(async d =>
                    {
                        try
                        {
                            return await processDonorInfoFuncAsync(d);
                        }
                        catch (TException e)
                        {
                            var failedDonorInfo = getFailedDonorInfo(d);

                            failedDonorIds.Add(string.IsNullOrEmpty(failedDonorInfo.DonorId) 
                                ? MissingDonorIdText 
                                : failedDonorInfo.DonorId);

                            logger.SendEvent(getEventModelFunc(new DonorProcessingException<TException>(failedDonorInfo, e)));

                            return default;
                        }
                    }
                ));

                return results.Where(d => d != null);
            }
            finally
            {
                if (failedDonorIds.Any())
                {
                    await SendFailureAlert(failedDonorIds);
                }
            }
        }

        private async Task SendFailureAlert(IEnumerable<string> failedDonorIds)
        {
            await notificationsClient.SendAlert(new Alert(
                alertSummary,
                $"Processing failed for donors: {string.Join(", ", failedDonorIds.Distinct())}. An event has been logged for each donor in Application Insights.",
                loggerPriority,
                NotificationConstants.OriginatorName
            ));
        }
    }
}
