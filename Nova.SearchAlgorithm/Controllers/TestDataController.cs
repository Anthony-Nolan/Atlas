using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
{
    /// <summary>
    /// These endpoints will not import genuine data but will
    /// create test data for manual testing.
    /// TODO:NOVA-1151 remove these endpoints before going into production
    /// </summary>
    public class TestDataController : ApiController
    {
        private readonly ITestDataService testDataService;

        public TestDataController(ITestDataService testDataService)
        {
            this.testDataService = testDataService;
        }

        [HttpPost]
        [Route("insert-single-donor")]
        public IHttpActionResult InsertSingleDonor()
        {
            testDataService.ImportSingleTestDonor();
            return Ok();
        }

        [HttpPost]
        [Route("insert-solar-donors")]
        public Task InsertSolarDonors()
        {
            return  testDataService.ImportSolarDonors();
        }

        [HttpPost]
        [Route("insert-all-solar-donors")]
        public Task InsertAllSolarDonors()
        {
            return testDataService.ImportAllDonorsFromSolar();
        }

        [HttpPost]
        [Route("insert-dummy-donors")]
        public IHttpActionResult InsertDummyDonors()
        {
            testDataService.ImportDummyData();
            return Ok();
        }
    }
}
