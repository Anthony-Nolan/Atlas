using Nova.SearchAlgorithm.Services.DonorImport;
using System.Threading.Tasks;
using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    public class HlaUpdateController : ApiController
    {
        private readonly IHlaUpdateService hlaUpdateService;

        public HlaUpdateController(IHlaUpdateService hlaUpdateService)
        {
            this.hlaUpdateService = hlaUpdateService;
        }

        [HttpPost]
        [Route("trigger-donor-hla-update")]
        public async Task TriggerSingleImport()
        {
            await hlaUpdateService.UpdateDonorHla();
        }
    }
}
