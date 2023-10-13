using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;
using Atlas.DonorImport.ApplicationInsights;

namespace Atlas.DonorImport.Services
{
    public interface IDonorImportMessageSender
    {
        Task SendSuccessMessage(string fileName, int importedDonorCount, IReadOnlyCollection<SearchableDonorValidationResult> failedDonors);
        Task SendFailureMessage(string fileName, ImportFailureReason failureReason, string failureReasonDescription);
    }
    internal class DonorImportMessageSender : IDonorImportMessageSender
    {
        private readonly ITopicClient topicClient;
        private readonly ILogger logger;

        public DonorImportMessageSender(ILogger logger, ITopicClientFactory topicClientFactory, MessagingServiceBusSettings messagingServiceBusSettings)
        {
            this.logger = logger;
            topicClient = topicClientFactory.BuildTopicClient(messagingServiceBusSettings.ConnectionString, messagingServiceBusSettings.DonorImportResultsTopic);
        }


        public async Task SendSuccessMessage(string fileName, int importedDonorCount, IReadOnlyCollection<SearchableDonorValidationResult> failedDonors)
        {
            var failedSummary = failedDonors.SelectMany(d => d.Errors)
                .GroupBy(e => e.ErrorMessage)
                .Select(g => new FauilureSummary
                {
                    Reason = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var donorImportMessage = new SuccessDonorImportMessage
            {
                FileName = fileName,
                ImportedDonorCount = importedDonorCount, 
                FailedDonorCount = failedDonors.Count,
                FailedDonorSummary = failedSummary
            };

            await Send(donorImportMessage, LogMessage);
        }

        public async Task SendFailureMessage(string fileName, ImportFailureReason failureReason, string failureReasonDescription)
        {
            var donorImportMessage = new FailedDonorImportMessage
            {
                FileName = fileName,
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

                var message = new Message(Encoding.UTF8.GetBytes(stringMessage))
                {
                    UserProperties =
                    {
                        { nameof(DonorImportMessage.WasSuccessful), donorImportMessage.WasSuccessful },
                        { nameof(DonorImportMessage.FileName), donorImportMessage.FileName }
                    }
                };

                await topicClient.SendAsync(message);
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorImportMessageSenderFailureEvent(e, stringMessage));
            }
        }

        private void LogMessage(SuccessDonorImportMessage donorImportMessage) =>
            logger.SendTrace($"{nameof(SuccessDonorImportMessage)} send.", LogLevel.Info, new Dictionary<string, string>
            {
                { nameof(donorImportMessage.FileName), donorImportMessage.FileName },
                { nameof(donorImportMessage.WasSuccessful), donorImportMessage.WasSuccessful.ToString() },
                { nameof(donorImportMessage.ImportedDonorCount), donorImportMessage.ImportedDonorCount.ToString() },
                { nameof(donorImportMessage.FailedDonorCount), donorImportMessage.FailedDonorCount.ToString() }
            });

        private void LogMessage(FailedDonorImportMessage donorImportMessage) =>
            logger.SendTrace($"{nameof(FailedDonorImportMessage)} send.", LogLevel.Info, new Dictionary<string, string>
            {
                { nameof(donorImportMessage.FileName), donorImportMessage.FileName },
                { nameof(donorImportMessage.WasSuccessful), donorImportMessage.WasSuccessful.ToString() },
                { nameof(donorImportMessage.FailureReason), donorImportMessage.FailureReason.ToString() },
                { nameof(donorImportMessage.FailureReasonDescription), donorImportMessage.FailureReasonDescription }
            });
    }
}
