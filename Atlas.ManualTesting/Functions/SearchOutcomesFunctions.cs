using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Functions
{
    public class SearchOutcomesFunctions
    {
        private readonly ISearchOutcomesProcessor searchOutcomesProcessor;

        public SearchOutcomesFunctions(ISearchOutcomesProcessor wmdaParallelRunResultsHandler)
        {
            this.searchOutcomesProcessor = wmdaParallelRunResultsHandler;
        }

        [FunctionName(nameof(GetSearchOutcomes))]
        public async Task<IActionResult> GetSearchOutcomes(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            SearchOutcomesPeekRequest request
        )
        {
            var resultedFileNames = await searchOutcomesProcessor.ProcessSearchMessages(request);
            return new JsonResult(new { resultedFileNames.PerformanceInfoFileName, resultedFileNames.FailedSearchesFileName, resultedFileNames.ProcessingErrorsFileName });
        }
    }
}
