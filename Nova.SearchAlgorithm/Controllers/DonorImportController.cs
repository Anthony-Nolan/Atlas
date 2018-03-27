using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services;

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
        public IHttpActionResult TriggerImport()
        {
            donorImportService.Import();
            return Ok();
        }
    }
}
