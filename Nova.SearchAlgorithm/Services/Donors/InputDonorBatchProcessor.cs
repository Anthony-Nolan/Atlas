using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Config;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;

namespace Nova.SearchAlgorithm.Services.Donors
{
    public interface IInputDonorBatchProcessor<T, out TException>
    {
        /// <param name="inputDonors">Batch of input donors to be processed.</param>
        /// <param name="processDonorFuncAsync">Function to be run on each input donor, that generates a return object of type T.</param>
        /// <param name="getEventModelFunc">Function to generate the processing failure model in the event that an exception of type TException is raised during processing."</param>
        /// <returns>Results from processing of the donor batch.</returns>
        Task<IEnumerable<T>> ProcessBatchAsync(
            IEnumerable<InputDonor> inputDonors,
            Func<InputDonor, Task<T>> processDonorFuncAsync,
            Func<TException, InputDonor, DonorProcessingFailureEventModel> getEventModelFunc);
    }

    public abstract class InputDonorBatchProcessor<T, TException> : IInputDonorBatchProcessor<T, TException>
        where TException : Exception
    {
        private readonly ILogger logger;
        private readonly INotificationsClient notificationsClient;
        private readonly Priority loggerPriority;
        private readonly string alertSummary;

        protected InputDonorBatchProcessor(
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

        public async Task<IEnumerable<T>> ProcessBatchAsync(
            IEnumerable<InputDonor> inputDonors,
            Func<InputDonor, Task<T>> processDonorFuncAsync,
            Func<TException, InputDonor, DonorProcessingFailureEventModel> getEventModelFunc)
        {
            inputDonors = inputDonors.ToList();
            if (!inputDonors.Any())
            {
                return new List<T>();
            }

            var failedDonorIds = new List<int>();

            try
            {
                var processedDonors = await Task.WhenAll(inputDonors.Select(async d =>
                    {
                        try
                        {
                            return await processDonorFuncAsync(d);
                        }
                        catch (TException e)
                        {
                            var eventModel = getEventModelFunc(e, d);
                            logger.SendEvent(eventModel);
                            failedDonorIds.Add(d.DonorId);
                            return default;
                        }
                    }
                ));

                return processedDonors.Where(d => d != null);
            }
            finally
            {
                if (failedDonorIds.Any())
                {
                    await SendFailureAlert(failedDonorIds);
                }
            }
        }

        private async Task SendFailureAlert(IEnumerable<int> failedDonorIds)
        {
            await notificationsClient.SendAlert(new Alert(
                alertSummary,
                $"Processing failed for donors: {string.Join(",", failedDonorIds)}. An event has been logged for each donor in Application Insights.",
                loggerPriority,
                NotificationConstants.OriginatorName
            ));
        }
    }
}
