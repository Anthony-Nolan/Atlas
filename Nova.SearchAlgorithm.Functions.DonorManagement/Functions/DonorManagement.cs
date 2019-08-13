using Microsoft.Azure.WebJobs;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.Utils.ApplicationInsights;
using System;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Functions
{
    public class DonorManagement
    {
        private readonly IDonorUpdateProcessor donorUpdateProcessor;
        private readonly ILogger logger;

        public DonorManagement(IDonorUpdateProcessor donorUpdateProcessor, ILogger logger)
        {
            this.donorUpdateProcessor = donorUpdateProcessor;
            this.logger = logger;
        }

        [FunctionName("ManageDonorByAvailability")]
        public async Task Run([TimerTrigger("%MessagingServiceBus.DonorManagement.CronSchedule%")] TimerInfo myTimer)
        {
            try
            {
                logger.SendTrace($"Commenced running of the donor management function.", LogLevel.Info);

                await donorUpdateProcessor.ProcessDonorUpdates();

                logger.SendTrace($"Completed running of the donor management function.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                logger.SendTrace($"Error when running the donor management function: " + ex.Message, LogLevel.Error);
                throw new DonorManagementException(ex);
            }
        }
    }
}