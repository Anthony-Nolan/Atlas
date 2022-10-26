using System;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Settings;

namespace Atlas.DonorImport.Services.DonorUpdates
{
    public interface IDonorUpdatesCleaner
    {
        /// <summary>
        /// Deletes records of updates that have been published and have a published date on or before the configured expiry date.
        /// If an expiry value has not been set (i.e., `null`), then no deletions will take place.
        /// </summary>
        Task DeleteExpiredPublishedDonorUpdates();
    }

    internal class DonorUpdatesCleaner : IDonorUpdatesCleaner
    {
        private readonly int? publishedUpdateExpiryInDays;
        private readonly IPublishableDonorUpdatesRepository updatesRepository;

        public DonorUpdatesCleaner(IPublishableDonorUpdatesRepository updatesRepository, PublishDonorUpdatesSettings settings)
        {
            this.updatesRepository = updatesRepository;
            publishedUpdateExpiryInDays = settings.PublishedUpdateExpiryInDays;
        }

        public async Task DeleteExpiredPublishedDonorUpdates()
        {
            if (publishedUpdateExpiryInDays == null)
            {
                return;
            }

            var cutOffDate = DateTimeOffset.Now.AddDays(-1*publishedUpdateExpiryInDays.Value);
            await updatesRepository.DeleteUpdatesPublishedOnOrBefore(cutOffDate);
        }
    }
}
