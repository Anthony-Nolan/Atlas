﻿using Atlas.SearchTracking.Settings.ServiceBus;
using System.Text;
using Atlas.SearchTracking.Enums;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.SearchTracking.Clients
{
    public interface ISearchTrackingServiceBusClient
    {
          Task PublishSearchTrackingEvent<TEvent>(TEvent tEvent, SearchTrackingEventType eventType);
    }

    public class SearchTrackingServiceBusClient : ISearchTrackingServiceBusClient
    {
        private readonly string connectionString;
        private readonly string searchTrackingEventsTopicName;
        private readonly string EventType = "EventType";

        public SearchTrackingServiceBusClient(MessagingServiceBusSettings messagingServiceBusSettings)
        {
            connectionString = messagingServiceBusSettings.ConnectionString;
            searchTrackingEventsTopicName = messagingServiceBusSettings.SearchTrackingEventsTopic;
        }

        public async Task PublishSearchTrackingEvent<TEvent>(TEvent tEvent, SearchTrackingEventType eventType)
        {
            var json = JsonConvert.SerializeObject(eventType);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties[EventType] = eventType.ToString();

            var client = new TopicClient(connectionString, searchTrackingEventsTopicName);
            await client.SendAsync(message);
        }
    }
}