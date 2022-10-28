using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Atlas.DonorImport.Services.DonorUpdates;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorUpdatesFunctions
    {
        private readonly IDonorUpdatesPublisher updatesPublisher;
        private readonly IDonorUpdatesCleaner updatesCleaner;

        public DonorUpdatesFunctions(IDonorUpdatesPublisher updatesPublisher, IDonorUpdatesCleaner updatesCleaner)
        {
            this.updatesPublisher = updatesPublisher;
            this.updatesCleaner = updatesCleaner;
        }

        [FunctionName(nameof(PublishSearchableDonorUpdates))]
        public async Task PublishSearchableDonorUpdates([TimerTrigger("%PublishDonorUpdates:PublishCronSchedule%")] TimerInfo timer)
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();
        }

        [FunctionName(nameof(DeleteExpiredPublishedDonorUpdates))]
        public async Task DeleteExpiredPublishedDonorUpdates([TimerTrigger("%PublishDonorUpdates:DeletionCronSchedule%")] TimerInfo timer)
        {
            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();
        }
    }
}