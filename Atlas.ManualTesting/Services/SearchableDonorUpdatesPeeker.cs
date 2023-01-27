using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus.Models;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.ServiceBus;

namespace Atlas.ManualTesting.Services
{
    public interface ISearchableDonorUpdatesPeeker
    {
        Task<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>> GetMessagesByAtlasDonorId(PeekByAtlasDonorIdsRequest peekRequest);
    }

    internal class SearchableDonorUpdatesPeeker : ISearchableDonorUpdatesPeeker
    {
        private readonly IMessagesPeeker<SearchableDonorUpdate> messagesReceiver;

        public SearchableDonorUpdatesPeeker(IMessagesPeeker<SearchableDonorUpdate> messagesReceiver)
        {
            this.messagesReceiver = messagesReceiver;
        }

        public async Task<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>> GetMessagesByAtlasDonorId(PeekByAtlasDonorIdsRequest peekRequest)
        {
            var notifications = await messagesReceiver.Peek(peekRequest);

            return notifications.Where(n => peekRequest.AtlasDonorIds.Contains(n.DeserializedBody.DonorId));
        }
    }
}