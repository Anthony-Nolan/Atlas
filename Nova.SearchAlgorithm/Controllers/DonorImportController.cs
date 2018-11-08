using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Services.DonorImport;

namespace Nova.SearchAlgorithm.Controllers
{
    public class DonorImportController : ApiController
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
