using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class DonorManagement
    {
        private readonly IDonorManagementService donorManagementService;

        public DonorManagement(IDonorManagementService donorManagementService)
        {
            this.donorManagementService = donorManagementService;
        }

        [FunctionName("ManageDonorByAvailability")]
        public async Task Run(
            [ServiceBusTrigger(
                "%MessagingServiceBus.DonorManagement.Topic%",
                "%MessagingServiceBus.DonorManagement.Subscription%",
                Connection = "MessagingServiceBus.ConnectionString")]
            string message)
        {
            var update = JsonConvert.DeserializeObject<SearchableDonorUpdateModel>(message);
            var donorAvailabilityUpdate = MapDonorAvailabilityUpdate(update);
            await donorManagementService.ManageDonorByAvailability(donorAvailabilityUpdate);
        }

        /// <summary>
        /// Map directly rather than using automapper to improve performance
        /// </summary>
        private static DonorAvailabilityUpdate MapDonorAvailabilityUpdate(SearchableDonorUpdateModel update)
        {
            if (int.TryParse(update.DonorId, out var donorId))
            {
                var donorAvailabilityUpdate = new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = update.SearchableDonorInformation?.ToInputDonor(),
                    IsAvailableForSearch = update.IsAvailableForSearch
                };
                return donorAvailabilityUpdate;
            };
            
            throw new DonorImportException($"Could not parse donor id: {update.DonorId} to an int");;
        }
    }
}