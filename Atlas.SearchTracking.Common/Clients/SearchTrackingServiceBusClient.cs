using System.Text;
using Atlas.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Config;
using Atlas.SearchTracking.Common.Enums;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Atlas.Common.Utils;
using System.Transactions;
using Atlas.Common.ServiceBus.Deprecated;

namespace Atlas.SearchTracking.Common.Clients
{
    public interface ISearchTrackingServiceBusClient
    {
          Task PublishSearchTrackingEvent<TEvent>(TEvent searchTrackingEvent, SearchTrackingEventType eventType);
    }

    public class SearchTrackingServiceBusClient : ISearchTrackingServiceBusClient
    {
        private readonly string connectionString;
        private readonly string searchTrackingTopicName;
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;
        private readonly ILogger logger;

        public SearchTrackingServiceBusClient(SearchTrackingServiceBusSettings searchTrackingServiceBusSettings, ILogger logger)
        {
            connectionString = searchTrackingServiceBusSettings.ConnectionString;
            searchTrackingTopicName = searchTrackingServiceBusSettings.SearchTrackingTopic;
            sendRetryCount = searchTrackingServiceBusSettings.SendRetryCount;
            sendRetryCooldownSeconds = searchTrackingServiceBusSettings.SendRetryCooldownSeconds;
            this.logger = logger;
        }

        public async Task PublishSearchTrackingEvent<TEvent>(TEvent searchTrackingEvent, SearchTrackingEventType eventType)
        {
            var json = JsonConvert.SerializeObject(searchTrackingEvent);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties[SearchTrackingConstants.EventType] = eventType.ToString();

            var client = new TopicClient(connectionString, searchTrackingTopicName);

            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await client.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace("SearchTracking " + eventType + " Exception " + exception.Message
                                                                 + " Attempt " + retryNumber, retryNumber == 1 ? LogLevel.Verbose : LogLevel.Warn));
            }
        }
    }
}
