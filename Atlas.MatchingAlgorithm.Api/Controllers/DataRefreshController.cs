using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.DataRefresh;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    public class DataRefreshController : ControllerBase
    {
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public DataRefreshController(IDonorImporter donorImporter, IHlaProcessor hlaProcessor, IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        [HttpPost]
        [Route("trigger-donor-import")]
        public async Task TriggerImport()
        {
            await donorImporter.ImportDonors();
        }
        
        [HttpPost]
        [Route("trigger-donor-hla-update")]
        public async Task TriggerSingleImport()
        {
            await hlaProcessor.UpdateDonorHla(wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }
    }
}
