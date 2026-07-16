using System.Threading.Tasks;
using Atlas.Common.Utils.Http;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.ManualTesting.Functions
{
    /// <summary>
    /// One-time operational utility (ATL-158) to replay searches that ran on the parallel ("Containers") match-prediction
    /// path and ended in a failed or incomplete state, re-dispatching them with <c>ParallelMatchPrediction = false</c>.
    /// </summary>
    public class ParallelMatchPredictionReplayFunctions
    {
        private readonly IFailedParallelSearchReplayer replayer;

        public ParallelMatchPredictionReplayFunctions(IFailedParallelSearchReplayer replayer)
        {
            this.replayer = replayer;
        }

        /// <summary>
        /// Call first with <c>DryRun = true</c> (the default) to review the candidate searches, then again with
        /// <c>DryRun = false</c> — optionally passing an explicit <c>SearchIdentifiers</c> allow-list — to re-dispatch.
        /// </summary>
        [Function(nameof(ReplayFailedParallelMatchPredictionSearches))]
        public async Task<IActionResult> ReplayFailedParallelMatchPredictionSearches(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(ParallelMatchPredictionReplayRequest), nameof(ParallelMatchPredictionReplayRequest))]
            HttpRequest request)
        {
            var replayRequest = await request.DeserialiseRequestBody<ParallelMatchPredictionReplayRequest>();
            var response = await replayer.ReplayFailedParallelSearches(replayRequest);
            return new JsonResult(response);
        }
    }
}
