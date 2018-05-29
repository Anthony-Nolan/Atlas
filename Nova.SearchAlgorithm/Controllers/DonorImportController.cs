using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
{
    /// <summary>
    /// These endpoints will not import genuine data but will
    /// create test data for manual testing.
    /// TODO:NOVA-1151 remove these endpoints before going into production
    /// </summary>
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
            donorImportService.ResumeDonorImport();
            return Ok();
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
