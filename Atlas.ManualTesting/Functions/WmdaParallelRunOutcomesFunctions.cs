using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Functions
{
    public class WmdaParallelRunOutcomesFunctions
    {
        private readonly IWmdaParallelRunResultsHandler wmdaParallelRunResultsHandler;

        public WmdaParallelRunOutcomesFunctions(IWmdaParallelRunResultsHandler wmdaParallelRunResultsHandler)
        {
            this.wmdaParallelRunResultsHandler = wmdaParallelRunResultsHandler;
        }

        [FunctionName(nameof(GetWmdaParallelRunOutcomes))]
        public async Task<IActionResult> GetWmdaParallelRunOutcomes(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            WmdaParallelSearchInfoPeekRequest request
        )
        {
            var resultedFileNames = await wmdaParallelRunResultsHandler.Handle(request);
            return new JsonResult(new { resultedFileNames.PerformanceInfoFileName, resultedFileNames.FailedSearchesFileName });
        }
    }
}
