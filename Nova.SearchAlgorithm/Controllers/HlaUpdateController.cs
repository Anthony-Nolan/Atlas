using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Services;

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
