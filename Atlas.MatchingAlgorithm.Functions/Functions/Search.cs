using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.Utils.CodeAnalysis;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class Search
    {
        private readonly ISearchDispatcher searchDispatcher;
        private readonly ISearchOrchestrator searchOrchestrator;

        public Search(ISearchDispatcher searchDispatcher, ISearchOrchestrator searchOrchestrator)
        {
            this.searchDispatcher = searchDispatcher;
            this.searchOrchestrator = searchOrchestrator;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(InitiateSearch))]
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

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RunSearch))]
        public async Task RunSearch(
            [ServiceBusTrigger("%MessagingServiceBus:SearchRequestsQueue%", Connection = "MessagingServiceBus:ConnectionString")]
            Message message)
        {
            var serialisedData = Encoding.UTF8.GetString(message.Body);
            var request = JsonConvert.DeserializeObject<IdentifiedSearchRequest>(serialisedData);

            await searchOrchestrator.RunSearch(request);
        }
    }
}