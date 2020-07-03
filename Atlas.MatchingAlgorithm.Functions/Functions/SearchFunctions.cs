using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.Search;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class SearchFunctions
    {
        private readonly ISearchDispatcher searchDispatcher;
        private readonly ISearchRunner searchRunner;

        public SearchFunctions(ISearchDispatcher searchDispatcher, ISearchRunner searchRunner)
        {
            this.searchDispatcher = searchDispatcher;
            this.searchRunner = searchRunner;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(InitiateSearch))]
        public async Task<IActionResult> InitiateSearch([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<MatchingRequest>(await new StreamReader(request.Body).ReadToEndAsync());
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

            await searchRunner.RunSearch(request);
        }
    }
}