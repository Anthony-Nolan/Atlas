using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Validation;
using Atlas.MatchingAlgorithm.Services.Search;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.Functions.PublicApi.Functions
{
    public class SearchFunctions
    {
        private readonly ISearchDispatcher searchDispatcher;

        public SearchFunctions(ISearchDispatcher searchDispatcher)
        {
            this.searchDispatcher = searchDispatcher;
        }
        
        [FunctionName(nameof(Search))]
        public async Task<IActionResult> Search([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            try
            {
                var id = await searchDispatcher.DispatchSearch(searchRequest);
                return new JsonResult(new SearchInitiationResponse {SearchIdentifier = id});
            }
            catch (ValidationException e)
            {
                return new BadRequestObjectResult(e.ToValidationErrorsModel());
            }
        }
    }
}