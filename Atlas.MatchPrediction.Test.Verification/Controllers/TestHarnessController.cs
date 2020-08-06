using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Controllers
{
    public class TestHarnessController : ControllerBase
    {
        private readonly ITestHarnessGenerator testHarnessGenerator;

        public TestHarnessController(ITestHarnessGenerator testHarnessGenerator)
        {
            this.testHarnessGenerator = testHarnessGenerator;
        }
        
        [HttpPost]
        [Route("test-harness")]
        public async Task<int> GenerateTestHarness([FromBody] GenerateTestHarnessRequest request)
        {
            return await testHarnessGenerator.GenerateTestHarness(request);
        }
    }
}