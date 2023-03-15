using System;
using System.Collections.Generic;
using System.Text;
using Atlas.Common.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Newtonsoft.Json;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;

namespace Atlas.DonorImport.Services
{
    public interface IDonorCheckerMessageSender
    {
        Task SendSuccessDonorIdCheckMessage(string requestFileLocation, int resultsCount, string resultsFilename);
        Task SendSuccessDonorInfoCheckMessage(string requestFileLocation, int resultsCount, string resultsFilename);
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

        public async Task SendSuccessDonorIdCheckMessage(string requestFileLocation, int resultsCount, string resultsFilename) =>
            await SendSuccessCheckMessage(idCheckerTopicClient, requestFileLocation, resultsCount, resultsFilename);

        public async Task SendSuccessDonorInfoCheckMessage(string requestFileLocation, int resultsCount, string resultsFilename) =>
            await SendSuccessCheckMessage(donorComparerTopicClient, requestFileLocation, resultsCount, resultsFilename);


        private async Task SendSuccessCheckMessage(ITopicClient topicClient, string requestFileLocation, int resultsCount, string resultsFilename)
        {
            var donorCheckerMessage = new DonorCheckerMessage(requestFileLocation, resultsCount, resultsFilename);
            var stringMessage = JsonConvert.SerializeObject(donorCheckerMessage);

            try
            {
                logger.SendTrace($"{nameof(DonorCheckerMessage)} sent.", LogLevel.Info, new Dictionary<string, string>
                {
                    { nameof(DonorCheckerMessage.RequestFileLocation), donorCheckerMessage.RequestFileLocation },
                    { nameof(DonorCheckerMessage.ResultsCount), donorCheckerMessage.ResultsCount.ToString() },
                    { nameof(DonorCheckerMessage.ResultsFilename), donorCheckerMessage.ResultsFilename },

                });
                var message = new Message(Encoding.UTF8.GetBytes(stringMessage));
                
                await topicClient.SendAsync(message);
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorCheckMessageSenderFailureEvent(e, stringMessage));
            }
        }
    }
}
