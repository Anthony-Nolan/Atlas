using Microsoft.Azure.WebJobs;
using Nova.SearchAlgorithm.Services.DonorManagement;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Functions
{
    public class DonorManagement
    {
        private readonly IDonorUpdateProcessor donorUpdateProcessor;
        
        public DonorManagement(IDonorUpdateProcessor donorUpdateProcessor)
        {
            this.donorUpdateProcessor = donorUpdateProcessor;
        }

        [FunctionName("ManageDonorByAvailability")]
        public async Task Run([TimerTrigger("%MessagingServiceBus.DonorManagement.CronSchedule%")] TimerInfo myTimer)
        {
            await donorUpdateProcessor.ProcessDonorUpdates();
        }
    }
}