using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Atlas.DonorImport.Services;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorUpdatesFunctions
    {
        private readonly IDonorUpdatesPublisher updatesPublisher;

        public DonorUpdatesFunctions(IDonorUpdatesPublisher updatesPublisher)
        {
            this.updatesPublisher = updatesPublisher;
        }

        [FunctionName(nameof(PublishSearchableDonorUpdates))]
        public async Task PublishSearchableDonorUpdates([TimerTrigger("%PublishDonorUpdates:CronSchedule%")] TimerInfo timer)
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();
        }
    }
}
