using System;
using System.Text;
using Atlas.Common.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;
using Newtonsoft.Json;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.FileSchema.Models.DonorComparer;

namespace Atlas.DonorImport.Services
{
    public interface IDonorCheckerMessageSender
    {
        Task SendSuccessCheckMessage(string requestFileLocation, string resultsFilename, int numberOfMismatches);
        Task SendSuccessDonorCompareMessage(string requestFileLocation, string resultsFilename, int numberOfMismatches);
    }

    internal class DonorCheckerMessageSender : IDonorCheckerMessageSender
    {
        private readonly ITopicClient idCheckerTopicClient;
        private readonly ITopicClient donorComparerTopicClient;
        private readonly ILogger logger;

        public DonorCheckerMessageSender(MessagingServiceBusSettings messagingServiceBusSettings, ITopicClientFactory topicClientFactory, ILogger logger)
        {
            this.logger = logger;
            idCheckerTopicClient = topicClientFactory.BuildTopicClient(messagingServiceBusSettings.ConnectionString,
                messagingServiceBusSettings.DonorIdCheckerResultsTopic);
            donorComparerTopicClient = topicClientFactory.BuildTopicClient(messagingServiceBusSettings.ConnectionString,
                messagingServiceBusSettings.CompareDonorsResultsTopic);
        }

        public async Task SendSuccessCheckMessage(string requestFileLocation, string resultsFilename, int numberOfMismatches)
        {
            var donorIdCheckerMessage = new DonorIdCheckerMessage(requestFileLocation, resultsFilename, numberOfMismatches);
            var stringMessage = JsonConvert.SerializeObject(donorIdCheckerMessage);

            try
            {
                logger.SendTrace($"{nameof(DonorIdCheckerMessage)} sent. {nameof(DonorIdCheckerMessage.Summary)}: {donorIdCheckerMessage.Summary}. {nameof(DonorIdCheckerMessage.RequestFileLocation)}: {donorIdCheckerMessage.RequestFileLocation}. {nameof(DonorIdCheckerMessage.ResultsFilename)}: {donorIdCheckerMessage.ResultsFilename}");
                var message = new Message(Encoding.UTF8.GetBytes(stringMessage));
                using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
                {
                    await idCheckerTopicClient.SendAsync(message);
                }
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorCheckMessageSenderFailureEvent(e, stringMessage));
            }
        }

        public async Task SendSuccessDonorCompareMessage(string requestFileLocation, string resultsFilename, int numberOfMismatches)
        {
            var donorComparerMessage = new DonorComparerMessage(requestFileLocation, resultsFilename, numberOfMismatches);
            var stringMessage = JsonConvert.SerializeObject(donorComparerMessage);

            try
            {
                logger.SendTrace($"{nameof(DonorComparerMessage)} sent. {nameof(DonorComparerMessage.Summary)}: {donorComparerMessage.Summary}. {nameof(DonorComparerMessage.RequestFileLocation)}: {donorComparerMessage.RequestFileLocation}. {nameof(DonorComparerMessage.ResultsFilename)}: {donorComparerMessage.ResultsFilename}");
                var message = new Message(Encoding.UTF8.GetBytes(stringMessage));
                using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
                {
                    await donorComparerTopicClient.SendAsync(message);
                }
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorCheckMessageSenderFailureEvent(e, stringMessage));
            }
        }
    }
}
