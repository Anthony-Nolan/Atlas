using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Controllers
{
    /// <summary>
    /// Debug endpoints to help during development of the verification framework - not designed to be used to run verification itself.
    /// </summary>
    [Route("debug")]
    public class DebugController : ControllerBase
    {
        private readonly INormalisedPoolGenerator poolGenerator;

        public DebugController(INormalisedPoolGenerator poolGenerator)
        {
            this.poolGenerator = poolGenerator;
        }


        [HttpPost]
        [Route("normalised-pool")]
        public async Task GenerateNormalisedPool()
        {
            await poolGenerator.GenerateNormalisedHaplotypeFrequencyPool();
        }
    }
}