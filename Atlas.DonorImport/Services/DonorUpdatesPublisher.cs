using System.Collections.Generic;
using Atlas.Common.ServiceBus;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.DonorImport.Services
{
    public interface IDonorUpdatesPublisher
    {
        /// <summary>
        /// Publishes a batch of donor updates, oldest first.
        /// The published batch is then deleted, so the next call will send out a new batch, if available.
        /// </summary>
        Task PublishSearchableDonorUpdatesBatch();
    }

    public class DonorUpdatesPublisher : IDonorUpdatesPublisher
    {
        // Currently set by SQL constraint on number of updates that can be deleted in one query.
        // If batch size must be increased, will need to delete updates in batches.
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
            await Delete(updates);
        }

        private async Task<IReadOnlyCollection<PublishableDonorUpdate>> GetUpdates()
        {
            var updates = await updatesRepository.GetOldestDonorUpdates(BatchSize);
            return updates.ToList();
        }

        private async Task Publish(IEnumerable<PublishableDonorUpdate> updates)
        {
            var updatesToPublish = updates.Select(u => u.ToSearchableDonorUpdate());
            await messagePublisher.BatchPublish(updatesToPublish);
        }

        private async Task Delete(IEnumerable<PublishableDonorUpdate> updates)
        {
            var updatesToDelete = updates.Select(u => u.Id);
            await updatesRepository.DeleteDonorUpdates(updatesToDelete);
        }
    }
}