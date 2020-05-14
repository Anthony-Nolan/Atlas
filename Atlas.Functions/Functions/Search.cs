using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.Utils.CodeAnalysis;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace Atlas.Functions.Functions
{
    public class Search
    {
        private readonly ISearchDispatcher searchDispatcher;

        public Search(ISearchDispatcher searchDispatcher)
        {
            this.searchDispatcher = searchDispatcher;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName("InitiateSearch")]
        public async Task<IActionResult> InitiateSearch([HttpTrigger] HttpRequest request)
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