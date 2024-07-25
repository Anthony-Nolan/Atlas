using System;
using System.Collections.Generic;
using System.Text;
using Atlas.Common.ServiceBus;
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

    internal sealed class DonorCheckerMessageSender : IDonorInfoCheckerMessageSender, IDonorIdCheckerMessageSender, IAsyncDisposable
    {
        private readonly ITopicClient topicClient;
        private readonly ILogger logger;

        public DonorCheckerMessageSender(ILogger logger, ITopicClientFactory topicClientFactory, string topicName)
        {
            this.logger = logger;
            topicClient = topicClientFactory.BuildTopicClient(topicName);
        }

        public ValueTask DisposeAsync() => topicClient.DisposeAsync();

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
                var message = new Azure.Messaging.ServiceBus.ServiceBusMessage(Encoding.UTF8.GetBytes(stringMessage));

                await topicClient.SendAsync(message);
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorCheckMessageSenderFailureEvent(e, stringMessage));
            }
        }
    }
}
