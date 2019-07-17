using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.DataRefresh;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class DataRefresh
    {
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;
        private readonly IDataRefreshOrchestrator dataRefreshOrchestrator;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public DataRefresh(
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor,
            IDataRefreshOrchestrator dataRefreshOrchestrator,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.dataRefreshOrchestrator = dataRefreshOrchestrator;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        [FunctionName("RunDataRefreshManual")]
        public async Task RunDataRefreshManual([HttpTrigger] HttpRequest httpRequest)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();
        }

        [FunctionName("RunDataRefresh")]
        public async Task RunDataRefresh([TimerTrigger("%DataRefresh.CronTab%")] TimerInfo timerInfo)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();
        }

        [FunctionName("RunDonorImport")]
        public async Task RunDonorImport([HttpTrigger] HttpRequest httpRequest)
        {
            await donorImporter.ImportDonors();
        }

        [FunctionName("ProcessDonorHla")]
        public async Task RunHlaRefresh([HttpTrigger] HttpRequest httpRequest)
        {
            await hlaProcessor.UpdateDonorHla(wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }
    }
}