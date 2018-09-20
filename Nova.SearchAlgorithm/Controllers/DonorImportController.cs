using Nova.SearchAlgorithm.Services.DonorImport;
using System.Threading.Tasks;
using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    public class DonorImportController : ApiController
    {
        private readonly IDonorImportService donorImportService;

        public DonorImportController(IDonorImportService donorImportService)
        {
            this.donorImportService = donorImportService;
        }

        [HttpPost]
        [Route("trigger-donor-import")]
        public async Task TriggerImport()
        {
            await donorImportService.StartDonorImport();
        }
    }
}
