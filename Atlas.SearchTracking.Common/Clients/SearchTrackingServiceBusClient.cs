using System.Text;
using Atlas.SearchTracking.Common.Config;
using Atlas.SearchTracking.Common.Enums;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Polly;

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

        public SearchTrackingServiceBusClient(SearchTrackingServiceBusSettings searchTrackingServiceBusSettings)
        {
            connectionString = searchTrackingServiceBusSettings.ConnectionString;
            searchTrackingTopicName = searchTrackingServiceBusSettings.SearchTrackingTopic;
            sendRetryCount = searchTrackingServiceBusSettings.SendRetryCount;
            sendRetryCooldownSeconds = searchTrackingServiceBusSettings.SendRetryCooldownSeconds;
        }

        public async Task PublishSearchTrackingEvent<TEvent>(TEvent searchTrackingEvent, SearchTrackingEventType eventType)
        {
            var json = JsonConvert.SerializeObject(searchTrackingEvent);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties[SearchTrackingConstants.EventType] = eventType.ToString();

            var retryPolicy = Policy
                .Handle<ServiceBusException>()
                .WaitAndRetryAsync(sendRetryCount, _ => TimeSpan.FromSeconds(sendRetryCooldownSeconds));

            var client = new TopicClient(connectionString, searchTrackingTopicName);
            await retryPolicy.ExecuteAsync(async () => await client.SendAsync(message));
        }
    }
}
