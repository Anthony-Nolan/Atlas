using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Clients
{
    internal interface IMessagingServiceBusClient
    {
        Task PublishDonorUpdateMessage(SearchableDonorUpdate donorUpdate);
        Task PublishDonorUpdateMessages(ICollection<SearchableDonorUpdate> donorUpdates);
    }

    internal class MessagingServiceBusClient : IMessagingServiceBusClient
    {
        private readonly ITopicClient donorUpdateTopicClient;

        public MessagingServiceBusClient(MessagingServiceBusSettings messagingServiceBusSettings, ITopicClientFactory topicClientFactory)
        {
            var connectionString = messagingServiceBusSettings.ConnectionString;
            var donorUpdateTopicName = messagingServiceBusSettings.MatchingDonorUpdateTopic;

            donorUpdateTopicClient = topicClientFactory.BuildTopicClient(connectionString, donorUpdateTopicName);
        }

        public async Task PublishDonorUpdateMessages(ICollection<SearchableDonorUpdate> donorUpdates)
        {
            foreach (var update in donorUpdates)
            {
                await PublishDonorUpdateMessage(update);
            }
        }

        public async Task PublishDonorUpdateMessage(SearchableDonorUpdate donorUpdate)
        {
            var json = JsonConvert.SerializeObject(donorUpdate);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                await donorUpdateTopicClient.SendAsync(message);
            }
        }
    }
}