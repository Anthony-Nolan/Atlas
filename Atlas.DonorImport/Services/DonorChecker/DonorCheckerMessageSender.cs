using System;
using System.Collections.Generic;
using System.Text;
using Atlas.Common.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Newtonsoft.Json;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;

namespace Atlas.DonorImport.Services.DonorChecker
{
    public interface IDonorCheckerMessageSender
    {
        Task SendSuccessDonorCheckMessage(string requestFileLocation, int resultsCount, string resultsFilename);
    }

    public interface IDonorInfoCheckerMessageSender : IDonorCheckerMessageSender { }
    public interface IDonorIdCheckerMessageSender : IDonorCheckerMessageSender { }

    internal class DonorCheckerMessageSender : IDonorInfoCheckerMessageSender, IDonorIdCheckerMessageSender
    {
        private readonly ITopicClient topicClient;
        private readonly ILogger logger;

        public DonorCheckerMessageSender(ILogger logger, ITopicClientFactory topicClientFactory, string connectionString, string topicName)
        {
            this.logger = logger;
            topicClient = topicClientFactory.BuildTopicClient(connectionString, topicName);
        }

        public async Task SendSuccessDonorCheckMessage(string requestFileLocation, int resultsCount, string resultsFilename)
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
