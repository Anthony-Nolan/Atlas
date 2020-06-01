using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    public class DataRefreshController : ControllerBase
    {
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;
        private readonly IActiveHlaVersionAccessor hlaVersionAccessor;

        public DataRefreshController(
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor,
            IActiveHlaVersionAccessor hlaVersionAccessor)
        {
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.hlaVersionAccessor = hlaVersionAccessor;
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
            await hlaProcessor.UpdateDonorHla(hlaVersionAccessor.GetActiveHlaNomenclatureVersion());
        }
    }
}
