using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nova.SearchAlgorithm.Services.DataRefresh;

namespace Nova.SearchAlgorithm.Api.Controllers
{
    public class DataRefreshController : ControllerBase
    {
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;

        public DataRefreshController(IDonorImporter donorImporter, IHlaProcessor hlaProcessor)
        {
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
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
            await hlaProcessor.UpdateDonorHla();
        }
    }
}
