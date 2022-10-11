using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus.Models;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.ServiceBus;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.ManualTesting.Services
{
    public interface ISearchableDonorUpdatesPeeker
    {
        Task<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>> GetMessagesByAtlasDonorId(PeekByAtlasDonorIdRequest peekRequest);
    }

    internal class SearchableDonorUpdatesPeeker : ISearchableDonorUpdatesPeeker
    {
        private readonly IMessagesPeeker<SearchableDonorUpdate> messagesReceiver;

        public SearchableDonorUpdatesPeeker(IMessagesPeeker<SearchableDonorUpdate> messagesReceiver)
        {
            this.messagesReceiver = messagesReceiver;
        }

        public async Task<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>> GetMessagesByAtlasDonorId(PeekByAtlasDonorIdRequest peekRequest)
        {
            var notifications = await messagesReceiver.Peek(peekRequest);

            return notifications.Where(n => n.DeserializedBody.DonorId == peekRequest.AtlasDonorId);
        }
    }
}