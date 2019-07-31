using System;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nova.DonorService.Client.Models.DonorUpdate;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus
{
    public interface IMessageReceiverService<T>
    {
        Task<IEnumerable<T>> ReceiveMessageBatch(int batchSize);
    }

    public class MessageReceiverService<T> : IMessageReceiverService<T>
    {
        private readonly MessageReceiver messageReceiver;

        public MessageReceiverService(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName)
        {
            messageReceiver = factory.GetMessageReceiver(topicName, subscriptionName);
        }

        public async Task<IEnumerable<T>> ReceiveMessageBatch(int batchSize)
        {
            var messageBatch = await messageReceiver.ReceiveAsync(batchSize);

            var deserializedMessages = new List<T>();
            foreach (var message in messageBatch)
            {
                deserializedMessages.Add(JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body)));
                await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
            }

            return deserializedMessages;
        }
    }
}
