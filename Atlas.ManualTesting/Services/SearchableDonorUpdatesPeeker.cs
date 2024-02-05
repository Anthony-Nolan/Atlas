using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Debugging;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.ManualTesting.Models;

namespace Atlas.ManualTesting.Services
{
    public interface ISearchableDonorUpdatesPeeker
    {
        Task<IEnumerable<SearchableDonorUpdate>> GetMessagesByAtlasDonorId(PeekByAtlasDonorIdsRequest peekRequest);
    }

    internal class SearchableDonorUpdatesPeeker : ISearchableDonorUpdatesPeeker
    {
        private readonly IMessagesPeeker<SearchableDonorUpdate> messagesReceiver;

        public SearchableDonorUpdatesPeeker(IMessagesPeeker<SearchableDonorUpdate> messagesReceiver)
        {
            this.messagesReceiver = messagesReceiver;
        }

        public async Task<IEnumerable<SearchableDonorUpdate>> GetMessagesByAtlasDonorId(
            PeekByAtlasDonorIdsRequest peekRequest)
        {
            var notifications = await messagesReceiver.Peek(peekRequest);

            return notifications.PeekedMessages.Where(n => peekRequest.AtlasDonorIds.Contains(n.DonorId));
        }
    }
}