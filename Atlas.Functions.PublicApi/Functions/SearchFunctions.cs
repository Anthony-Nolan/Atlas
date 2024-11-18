using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.RepeatSearch.Services.Search;
using Atlas.RepeatSearch.Validators;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using FluentValidation.Results;
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
        private readonly IRepeatSearchDispatcher repeatSearchDispatcher;
        private readonly IMatchPredictionValidator matchPredictionValidator;

        public SearchFunctions(ISearchDispatcher searchDispatcher, IRepeatSearchDispatcher repeatSearchDispatcher, IMatchPredictionValidator matchPredictionValidator)
        {
            this.searchDispatcher = searchDispatcher;
            this.repeatSearchDispatcher = repeatSearchDispatcher;
            this.matchPredictionValidator = matchPredictionValidator;
        }

        [FunctionName(nameof(Search))]
        public async Task<IActionResult> Search(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(SearchRequest), nameof(SearchRequest))]
            HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            var matchingValidationResult = await new SearchRequestValidator().ValidateAsync(searchRequest);
            if (!matchingValidationResult.IsValid)
            {
                return BuildValidationResponse(matchingValidationResult);
            }

            var probabilityRequestToValidate = searchRequest.ToPartialMatchProbabilitySearchRequest();
            var probabilityValidationResult = matchPredictionValidator.ValidateMatchProbabilityNonDonorInput(probabilityRequestToValidate);
            if (!probabilityValidationResult.IsValid)
            {
                return BuildValidationResponse(probabilityValidationResult);
            }

            var id = await searchDispatcher.DispatchSearch(searchRequest);
            return new JsonResult(new SearchInitiationResponse {SearchIdentifier = id});
        }

        [FunctionName(nameof(RepeatSearch))]
        public async Task<IActionResult> RepeatSearch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(RepeatSearchRequest), nameof(RepeatSearchRequest))]
            HttpRequest request)
        {
            var repeatSearchRequest = JsonConvert.DeserializeObject<RepeatSearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            var matchingValidationResult = await new RepeatSearchRequestValidator().ValidateAsync(repeatSearchRequest);
            if (!matchingValidationResult.IsValid)
            {
                return BuildValidationResponse(matchingValidationResult);
            }

            var probabilityRequestToValidate = repeatSearchRequest.SearchRequest.ToPartialMatchProbabilitySearchRequest();
            var probabilityValidationResult = matchPredictionValidator.ValidateMatchProbabilityNonDonorInput(probabilityRequestToValidate);
            if (!probabilityValidationResult.IsValid)
            {
                return BuildValidationResponse(probabilityValidationResult);
            }

            var repeatSearchId = await repeatSearchDispatcher.DispatchSearch(repeatSearchRequest);
            return new JsonResult(new SearchInitiationResponse
            {
                SearchIdentifier = repeatSearchRequest.OriginalSearchId,
                RepeatSearchIdentifier = repeatSearchId
            });
        }

        private static IActionResult BuildValidationResponse(ValidationResult validationResult) =>
            new BadRequestObjectResult(validationResult.Errors);
    }
}