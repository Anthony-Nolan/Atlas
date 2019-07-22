using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nova.SearchAlgorithm.Services.DonorImport;
using Nova.SearchAlgorithm.Services.DonorImport.PreProcessing;

namespace Nova.SearchAlgorithm.Api.Controllers
{
    public class DonorImportController : ControllerBase
    {
        private readonly IDonorImportService donorImportService;
        private readonly IHlaUpdateService hlaUpdateService;

        public DonorImportController(IDonorImportService donorImportService, IHlaUpdateService hlaUpdateService)
        {
            this.donorImportService = donorImportService;
            this.hlaUpdateService = hlaUpdateService;
        }

        [HttpPost]
        [Route("trigger-donor-import")]
        public async Task TriggerImport()
        {
            await donorImportService.StartDonorImport();
        }
        
        [HttpPost]
        [Route("trigger-donor-hla-update")]
        public async Task TriggerSingleImport()
        {
            await hlaUpdateService.UpdateDonorHla();
        }
    }
}
