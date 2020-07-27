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
        public async Task<int> GenerateTestHarness()
        {
            return await testHarnessGenerator.GenerateTestHarness();
        }
    }
}