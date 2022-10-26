using System.Collections.Generic;
using Atlas.Common.ServiceBus;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.DonorImport.Services.DonorUpdates
{
    public interface IDonorUpdatesPublisher
    {
        /// <summary>
        /// Publishes a batch of donor updates, oldest first.
        /// The current batch is then marked as published, so the next call will send out a new batch, if available.
        /// </summary>
        Task PublishSearchableDonorUpdatesBatch();
    }

    public class DonorUpdatesPublisher : IDonorUpdatesPublisher
    {
        // Current set value set by SQL constraint on the number of update IDs
        // that can be submitted in one parameterized query.
        private const int BatchSize = 2000;

        private readonly IMessageBatchPublisher<SearchableDonorUpdate> messagePublisher;
        private readonly IPublishableDonorUpdatesRepository updatesRepository;

        public DonorUpdatesPublisher(
            IMessageBatchPublisher<SearchableDonorUpdate> messagePublisher,
            IPublishableDonorUpdatesRepository updatesRepository)
        {
            this.messagePublisher = messagePublisher;
            this.updatesRepository = updatesRepository;
        }

        public async Task PublishSearchableDonorUpdatesBatch()
        {
            var updates = await GetUpdates();

            if (updates.IsNullOrEmpty())
            {
                return;
            }

            await Publish(updates);
        }

        private async Task<IReadOnlyCollection<PublishableDonorUpdate>> GetUpdates()
        {
            var updates = await updatesRepository.GetOldestUnpublishedDonorUpdates(BatchSize);
            return updates.ToList();
        }

        private async Task Publish(IReadOnlyCollection<PublishableDonorUpdate> updates)
        {
            var updatesToPublish = updates.Select(u => u.ToSearchableDonorUpdate());
            await messagePublisher.BatchPublish(updatesToPublish);
            await updatesRepository.MarkUpdatesAsPublished(updates.Select(u => u.Id));
        }
    }
}