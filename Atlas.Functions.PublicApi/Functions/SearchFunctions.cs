using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Functions.PublicApi.Settings;
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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.PublicApi.Functions
{
    public class SearchFunctions
    {
        private readonly ISearchDispatcher searchDispatcher;
        private readonly IRepeatSearchDispatcher repeatSearchDispatcher;
        private readonly IMatchPredictionValidator matchPredictionValidator;
        private readonly bool defaultParallelMatchPrediction;
        private readonly int parallelMatchPredictionRequestPercentage;

        public SearchFunctions(
            ISearchDispatcher searchDispatcher,
            IRepeatSearchDispatcher repeatSearchDispatcher,
            IMatchPredictionValidator matchPredictionValidator,
            IOptions<SearchFunctionSettings> searchFunctionSettings)
        {
            this.searchDispatcher = searchDispatcher;
            this.repeatSearchDispatcher = repeatSearchDispatcher;
            this.matchPredictionValidator = matchPredictionValidator;
            defaultParallelMatchPrediction = searchFunctionSettings.Value.DefaultParallelMatchPrediction;
            parallelMatchPredictionRequestPercentage = Math.Clamp(searchFunctionSettings.Value.ParallelMatchPredictionRequestPercentage, 0, 100);
        }

        [Function(nameof(Search))]
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

            ResolveParallelMatchPrediction(searchRequest);

            var id = await searchDispatcher.DispatchSearch(searchRequest);
            await searchDispatcher.DispatchSearchTrackingEvent(searchRequest, id);

            return new JsonResult(new SearchInitiationResponse {SearchIdentifier = id});
        }

        [Function(nameof(RepeatSearch))]
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

            ResolveParallelMatchPrediction(repeatSearchRequest.SearchRequest);

            var repeatSearchId = await repeatSearchDispatcher.DispatchSearch(repeatSearchRequest);
            await repeatSearchDispatcher.DispatchSearchTrackingEvent(repeatSearchRequest, repeatSearchId);

            return new JsonResult(new SearchInitiationResponse
            {
                SearchIdentifier = repeatSearchRequest.OriginalSearchId,
                RepeatSearchIdentifier = repeatSearchId
            });
        }

        /// <summary>
        /// Resolves <see cref="SearchRequest.ParallelMatchPrediction"/> to a concrete value and then applies the canary
        /// throttle. A request with no explicit value falls back to <see cref="SearchFunctionSettings.DefaultParallelMatchPrediction"/>.
        /// Once resolved to <c>true</c>, only <see cref="SearchFunctionSettings.ParallelMatchPredictionRequestPercentage"/>
        /// percent of those requests keep the parallel ("Containers") path; the remainder are demoted to <c>false</c> so
        /// they take the legacy sequential Durable orchestrator path. A request that resolves to <c>false</c> is never
        /// promoted onto the parallel path.
        /// </summary>
        private void ResolveParallelMatchPrediction(SearchRequest searchRequest)
        {
            searchRequest.ParallelMatchPrediction ??= defaultParallelMatchPrediction;

            if (searchRequest.ParallelMatchPrediction == true
                && Random.Shared.Next(100) >= parallelMatchPredictionRequestPercentage)
            {
                searchRequest.ParallelMatchPrediction = false;
            }
        }

        private static IActionResult BuildValidationResponse(ValidationResult validationResult) =>
            new BadRequestObjectResult(validationResult.Errors);
    }
}