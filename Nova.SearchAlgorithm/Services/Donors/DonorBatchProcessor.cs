using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Config;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    public interface IDonorBatchProcessor<TDonor, TResult, out TException>
        where TException : Exception
    {
        /// <param name="donorInfo">Batch of donor info of type TDonor to be processed.</param>
        /// <param name="processDonorInfoFuncAsync">Function to be run on each donor info, that generates an object of type TResult.</param>
        /// <param name="getEventModelFunc">Function to generate the processing failure model in the event that an exception of type TException is raised during processing."</param>
        /// <param name="getDonorIdFunc">Function to select donor id from the donor info object.</param>
        /// <returns>Results from processing of the donor batch.</returns>
        Task<IEnumerable<TResult>> ProcessBatchAsync(
            IEnumerable<TDonor> donorInfo,
            Func<TDonor, Task<TResult>> processDonorInfoFuncAsync,
            Func<TException, TDonor, DonorProcessingFailureEventModel> getEventModelFunc,
            Func<TDonor, string> getDonorIdFunc);
    }

    public abstract class DonorBatchProcessor<TDonor, TResult, TException> : IDonorBatchProcessor<TDonor, TResult, TException>
        where TException : Exception
    {
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
            Func<TException, TDonor, DonorProcessingFailureEventModel> getEventModelFunc,
            Func<TDonor, string> getDonorIdFunc)
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
                            logger.SendEvent(getEventModelFunc(e, d));
                            failedDonorIds.Add(getDonorIdFunc(d));
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
                $"Processing failed for donors: {string.Join(",", failedDonorIds.Distinct())}. An event has been logged for each donor in Application Insights.",
                loggerPriority,
                NotificationConstants.OriginatorName
            ));
        }
    }
}
