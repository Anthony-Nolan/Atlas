using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.Search;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.Functions.Functions
{
    public class SearchFunctions
    {
        private readonly ISearchDispatcher searchDispatcher;

        public SearchFunctions(ISearchDispatcher searchDispatcher)
        {
            this.searchDispatcher = searchDispatcher;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(InitiateSearch))]
        public async Task<IActionResult> InitiateSearch([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
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