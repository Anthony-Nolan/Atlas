using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;
using Atlas.DonorImport.ApplicationInsights;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Atlas.Common.Utils;
using System.Transactions;

namespace Atlas.DonorImport.Services
{
    public interface IDonorImportMessageSender
    {
        Task SendSuccessMessage(string fileName, int importedDonorCount, IReadOnlyCollection<SearchableDonorValidationResult> failedDonors);
        Task SendFailureMessage(string fileName, ImportFailureReason failureReason, string failureReasonDescription);
    }
    internal sealed class DonorImportMessageSender : IDonorImportMessageSender, IAsyncDisposable
    {
        private readonly ITopicClient topicClient;
        private readonly ILogger logger;
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;

        public DonorImportMessageSender(ILogger logger, [FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory, MessagingServiceBusSettings messagingServiceBusSettings)
        {
            this.logger = logger;
            topicClient = topicClientFactory.BuildTopicClient(messagingServiceBusSettings.DonorImportResultsTopic);
            sendRetryCount = messagingServiceBusSettings.SendRetryCount;
            sendRetryCooldownSeconds = messagingServiceBusSettings.SendRetryCooldownSeconds;
        }


        public async Task SendSuccessMessage(string fileName, int importedDonorCount, IReadOnlyCollection<SearchableDonorValidationResult> failedDonors)
        {
            var failedSummary = failedDonors.SelectMany(d => d.Errors)
                .GroupBy(e => e.ErrorMessage)
                .Select(g => new FailureSummary
                {
                    Reason = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var donorImportMessage = new DonorImportMessage
            {
                FileName = fileName,
                WasSuccessful = true,
                SuccessfulImportInfo = new SuccessfulImportInfo
                {
                    ImportedDonorCount = importedDonorCount,
                    FailedDonorCount = failedDonors.Count,
                    FailedDonorSummary = failedSummary
                },
                // TODO: stop setting these obsolete properties in Atlas v.1.8.0
                ImportedDonorCount = importedDonorCount,
                FailedDonorCount = failedDonors.Count,
                FailedDonorSummary = failedSummary
            };

            await Send(donorImportMessage, LogMessage);
        }

        public async Task SendFailureMessage(string fileName, ImportFailureReason failureReason, string failureReasonDescription)
        {
            var donorImportMessage = new DonorImportMessage
            {
                FileName = fileName,
                WasSuccessful = false,
                FailedImportInfo = new FailedImportInfo
                {
                    FileFailureReason = failureReason,
                    FileFailureDescription = failureReasonDescription
                },
                // TODO: stop setting these obsolete properties in Atlas v.1.8.0
                FailureReason = failureReason,
                FailureReasonDescription = failureReasonDescription
            };

            await Send(donorImportMessage, LogMessage);
        }

        private async Task Send<T>(T donorImportMessage, Action<T> logMessage) where T : DonorImportMessage
        {
            var stringMessage = JsonConvert.SerializeObject(donorImportMessage);

            try
            {
                logMessage(donorImportMessage);

                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(stringMessage))
                {
                    ApplicationProperties =
                    {
                        { nameof(DonorImportMessage.WasSuccessful), donorImportMessage.WasSuccessful },
                        { nameof(DonorImportMessage.FileName), donorImportMessage.FileName }
                    }
                };

                await topicClient.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send donor import message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorImportMessageSenderFailureEvent(e, stringMessage));
            }
        }

        private void LogMessage(DonorImportMessage donorImportMessage) =>
            logger.SendTrace($"{nameof(DonorImportMessage)} send.", LogLevel.Info, new Dictionary<string, string>
            {
                { nameof(donorImportMessage.FileName), donorImportMessage.FileName },
                { nameof(donorImportMessage.WasSuccessful), donorImportMessage.WasSuccessful.ToString() },
                { nameof(donorImportMessage.SuccessfulImportInfo.ImportedDonorCount), donorImportMessage.SuccessfulImportInfo?.ImportedDonorCount.ToString() },
                { nameof(donorImportMessage.SuccessfulImportInfo.FailedDonorCount), donorImportMessage.SuccessfulImportInfo?.FailedDonorCount.ToString() },
                { nameof(donorImportMessage.FailedImportInfo.FileFailureReason), donorImportMessage.FailedImportInfo?.FileFailureReason.ToString() },
                { nameof(donorImportMessage.FailedImportInfo.FileFailureDescription), donorImportMessage.FailedImportInfo?.FileFailureDescription }
            });

        public ValueTask DisposeAsync() => topicClient.DisposeAsync();
    }
}
