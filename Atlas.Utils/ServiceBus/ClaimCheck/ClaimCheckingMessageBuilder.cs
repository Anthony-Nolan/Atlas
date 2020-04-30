using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Atlas.Utils.Storage;
using Atlas.Utils.Storage.ClaimCheck;

namespace Atlas.Utils.ServiceBus.ClaimCheck
{
    public interface IClaimCheckingMessageBuilder
    {
        Task<Message> BuildMessage<T>(T payload, bool claimCheck, string desiredKey = null);
        Task<Message> BuildMessage(string payload, bool claimCheck, string desiredKey = null);
    }

    public class ClaimCheckingMessageBuilder : IClaimCheckingMessageBuilder
    {
        private readonly IStorageClient claimCheckClient;
        private readonly string containerName;

        public ClaimCheckingMessageBuilder(IStorageClient claimCheckClient, string claimCheckContainer)
        {
            this.claimCheckClient = claimCheckClient;
            containerName = claimCheckContainer;
        }

        public async Task<Message> BuildMessage<T>(T payload, bool claimCheck, string desiredKey = null)
        {
            var serializedPayload = JsonConvert.SerializeObject(payload);
            return await BuildMessage(serializedPayload, claimCheck, desiredKey);
        }

        public async Task<Message> BuildMessage(string serializedPayload, bool claimCheck, string desiredKey = null)
        {
            if (claimCheck)
            {
                var filename = desiredKey ?? Guid.NewGuid().ToString();
                await claimCheckClient.Upload(containerName, filename, serializedPayload);
                serializedPayload = JsonConvert.SerializeObject(new ClaimCheckNotification { ClaimId = filename });
            }
            var message = new Message(Encoding.UTF8.GetBytes(serializedPayload));
            message.UserProperties.Add(ClaimCheckConstants.IsCheckedPropertyKey, claimCheck);
            return message;
        }
    }
}
