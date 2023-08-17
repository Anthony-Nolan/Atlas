using System;
using System.Collections.Generic;
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
        Task SendSuccessMessage(string fileName, int importedDonorCount, int failedDonorCount);
        Task SendFailureMessage(string fileName, ImportFaulireReason failureReason, string failureReasonDescription);
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


        public async Task SendSuccessMessage(string fileName, int importedDonorCount, int failedDonorCount)
        {
            var donorImportMessage = new SuccessDonorImportMessage
            {
                FileName = fileName,
                ImportedDonorCount = importedDonorCount, 
                FailedDonorCount = failedDonorCount
            };
            var stringMessage = JsonConvert.SerializeObject(donorImportMessage);

            try
            {
                logger.SendTrace($"{nameof(SuccessDonorImportMessage)} send.", LogLevel.Info, new Dictionary<string, string>
                {
                    { nameof(donorImportMessage.FileName), donorImportMessage.FileName },
                    { nameof(donorImportMessage.WasSuccessful), donorImportMessage.WasSuccessful.ToString() },
                    { nameof(donorImportMessage.ImportedDonorCount), donorImportMessage.ImportedDonorCount.ToString() },
                    { nameof(donorImportMessage.FailedDonorCount), donorImportMessage.FailedDonorCount.ToString() }
                });
                var message = new Message(Encoding.UTF8.GetBytes(stringMessage));

                await topicClient.SendAsync(message);
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorImportMessageSenderFailureEvent(e, stringMessage));
            }
        }

        public async Task SendFailureMessage(string fileName, ImportFaulireReason failureReason, string failureReasonDescription)
        {
            var donorImportMessage = new FailedDonorImportMessage
            {
                FileName = fileName,
                FailureReason = failureReason,
                FailureReasonDescription = failureReasonDescription
            };
            var stringMessage = JsonConvert.SerializeObject(donorImportMessage);

            try
            {
                logger.SendTrace($"{nameof(SuccessDonorImportMessage)} send.", LogLevel.Info, new Dictionary<string, string>
                {
                    { nameof(donorImportMessage.FileName), donorImportMessage.FileName },
                    { nameof(donorImportMessage.WasSuccessful), donorImportMessage.WasSuccessful.ToString() },
                    { nameof(donorImportMessage.FailureReason), donorImportMessage.FailureReason.ToString() },
                    { nameof(donorImportMessage.FailureReasonDescription), donorImportMessage.FailureReasonDescription }
                });
                var message = new Message(Encoding.UTF8.GetBytes(stringMessage));

                await topicClient.SendAsync(message);
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorImportMessageSenderFailureEvent(e, stringMessage));
            }
        }
    }
}
