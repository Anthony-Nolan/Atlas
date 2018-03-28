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
        [Route("trigger-single-donor-import")]
        public IHttpActionResult TriggerSingleImport()
        {
            donorImportService.ImportSingleTestDonor();
            return Ok();
        }

        [HttpPost]
        [Route("trigger-solar-donor-import")]
        public IHttpActionResult TriggerSolarImport()
        {
            donorImportService.ImportTenSolarDonors();
            return Ok();
        }

        [HttpPost]
        [Route("trigger-dummy-donor-import")]
        public IHttpActionResult TriggerDummyImport()
        {
            donorImportService.ImportDummyData();
            return Ok();
        }
    }
}
