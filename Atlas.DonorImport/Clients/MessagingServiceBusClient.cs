using System;
using System.Text;
using System.Threading.Tasks;
using Atlas.DonorImport.Settings.ServiceBus;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Clients
{
    public interface IMessagingServiceBusClient
    {
        Task PublishDonorUpdateMessage(SearchableDonorUpdate donorUpdate);
    }

    public class MessagingServiceBusClient : IMessagingServiceBusClient
    {
        private readonly TopicClient donorUpdateTopicClient;

        public MessagingServiceBusClient(IOptions<MessagingServiceBusSettings> messagingServiceBusSettings)
        {
            var connectionString = messagingServiceBusSettings.Value.ConnectionString;
            var donorUpdateTopicName = messagingServiceBusSettings.Value.MatchingDonorUpdateTopic;

            donorUpdateTopicClient = new TopicClient(connectionString, donorUpdateTopicName);
        }

        public async Task PublishDonorUpdateMessage(SearchableDonorUpdate donorUpdate)
        {
            var json = JsonConvert.SerializeObject(donorUpdate);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            await donorUpdateTopicClient.SendAsync(message);
        }
    }
}