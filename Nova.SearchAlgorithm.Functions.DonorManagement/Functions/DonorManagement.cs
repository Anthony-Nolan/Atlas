using Microsoft.Azure.WebJobs;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Services.DonorManagement;
using System;
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
            try
            {
                await donorUpdateProcessor.ProcessDonorUpdates();
            }
            catch (Exception ex)
            {
                throw new ManageDonorFunctionException(ex);
            }
        }
    }
}