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
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    public interface IDonorIdCheckerMessageSender
    {
        Task SendSuccessCheckMessage(string requestFileLocation, string resultsFilename);
    }

    internal class DonorIdCheckerMessageSender : IDonorIdCheckerMessageSender
    {
        private readonly ITopicClient topicClient;
        private readonly ILogger logger;

        public DonorIdCheckerMessageSender(MessagingServiceBusSettings messagingServiceBusSettings, ITopicClientFactory topicClientFactory, ILogger logger)
        {
            this.logger = logger;
            topicClient = topicClientFactory.BuildTopicClient(messagingServiceBusSettings.ConnectionString,
                messagingServiceBusSettings.DonorIdCheckerResultsTopic);
        }

        public async Task SendSuccessCheckMessage(string requestFileLocation, string resultsFilename)
        {
            var donorIdCheckerMessage = new DonorIdCheckerMessage($"Donor Id(s) check was finished: {requestFileLocation}", resultsFilename);
            var stringMessage = JsonConvert.SerializeObject(donorIdCheckerMessage);

            try
            {
                logger.SendTrace($"{nameof(DonorIdCheckerMessage)} sent. {nameof(DonorIdCheckerMessage.Summary)}: {donorIdCheckerMessage.Summary}. {nameof(DonorIdCheckerMessage.ResultsFilename)}: {donorIdCheckerMessage.ResultsFilename}");
                var message = new Message(Encoding.UTF8.GetBytes(stringMessage));
                using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
                {
                    await topicClient.SendAsync(message);
                }
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorIdCheckMessageSenderFailureEvent(e, stringMessage));
            }
        }
    }
}
