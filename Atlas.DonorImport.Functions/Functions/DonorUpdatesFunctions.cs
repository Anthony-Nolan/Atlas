using System.Threading.Tasks;
using Atlas.DonorImport.Services.DonorUpdates;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorUpdatesFunctions
    {
        private readonly IDonorUpdatesPublisher updatesPublisher;
        private readonly IDonorUpdatesCleaner updatesCleaner;
        private readonly IDonorUpdatesSaver donorUpdatesSaver;

        public DonorUpdatesFunctions(
            IDonorUpdatesPublisher updatesPublisher,
            IDonorUpdatesCleaner updatesCleaner,
            IDonorUpdatesSaver donorUpdatesSaver)
        {
            this.updatesPublisher = updatesPublisher;
            this.updatesCleaner = updatesCleaner;
            this.donorUpdatesSaver = donorUpdatesSaver;
        }

        [Function(nameof(PublishSearchableDonorUpdates))]
        public async Task PublishSearchableDonorUpdates([TimerTrigger("%PublishDonorUpdates:PublishCronSchedule%")] TimerInfo timer)
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();
        }

        [Function(nameof(DeleteExpiredPublishedDonorUpdates))]
        public async Task DeleteExpiredPublishedDonorUpdates([TimerTrigger("%PublishDonorUpdates:DeletionCronSchedule%")] TimerInfo timer)
        {
            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();
        }

        [Function(nameof(ManuallyPublishDonorUpdatesByDonorId))]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ManuallyPublishDonorUpdatesByDonorId(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(int[]), "Donor ids")] 
            HttpRequest request)
        {
            using var reader = new StreamReader(request.Body);
            var ids = JsonConvert.DeserializeObject<int[]>(await reader.ReadToEndAsync());

            await donorUpdatesSaver.GenerateAndSave(ids);
            return new OkResult();
        }
    }
}