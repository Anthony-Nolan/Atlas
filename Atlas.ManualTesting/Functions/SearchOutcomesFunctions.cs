using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;

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
            [RequestBodyType(typeof(SearchOutcomesPeekRequest), nameof(SearchOutcomesPeekRequest))]
            HttpRequest request
        )
        {
            var peekRequest = JsonConvert.DeserializeObject<SearchOutcomesPeekRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var resultedFileNames = await searchOutcomesProcessor.ProcessSearchMessages(peekRequest);
            return new JsonResult(new { resultedFileNames.PerformanceInfoFileName, resultedFileNames.FailedSearchesFileName, resultedFileNames.ProcessingErrorsFileName });
        }
    }
}
