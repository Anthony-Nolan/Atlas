using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.PublicApi.Models;
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
        internal const string PrecomputedGenotypeSetsChosenEventName = "Precomputed genotype sets method chosen";

        private readonly ISearchDispatcher searchDispatcher;
        private readonly IRepeatSearchDispatcher repeatSearchDispatcher;
        private readonly IMatchPredictionValidator matchPredictionValidator;
        private readonly bool defaultParallelMatchPrediction;
        private readonly int parallelMatchPredictionRequestPercentage;
        private readonly bool defaultUsePrecomputedGenotypeSets;
        private readonly int usePrecomputedGenotypeSetsRequestPercentage;
        private readonly IAtlasLogger logger;

        public SearchFunctions(
            ISearchDispatcher searchDispatcher,
            IRepeatSearchDispatcher repeatSearchDispatcher,
            IMatchPredictionValidator matchPredictionValidator,
            IOptions<SearchFunctionSettings> searchFunctionSettings,
            IAtlasLogger logger)
        {
            this.searchDispatcher = searchDispatcher;
            this.repeatSearchDispatcher = repeatSearchDispatcher;
            this.matchPredictionValidator = matchPredictionValidator;
            defaultParallelMatchPrediction = searchFunctionSettings.Value.DefaultParallelMatchPrediction;
            parallelMatchPredictionRequestPercentage = Math.Clamp(searchFunctionSettings.Value.ParallelMatchPredictionRequestPercentage, 0, 100);
            defaultUsePrecomputedGenotypeSets = searchFunctionSettings.Value.DefaultUsePrecomputedGenotypeSets;
            usePrecomputedGenotypeSetsRequestPercentage = Math.Clamp(searchFunctionSettings.Value.UsePrecomputedGenotypeSetsRequestPercentage, 0, 100);
            this.logger = logger;
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
            var precomputedGenotypeSetsSource = ResolveUsePrecomputedGenotypeSets(searchRequest);

            var id = await searchDispatcher.DispatchSearch(searchRequest);
            LogPrecomputedGenotypeSetsChoice(searchRequest, precomputedGenotypeSetsSource, id, null);
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
            // Chosen again: a repeat search does not reuse the method of its original search.
            var precomputedGenotypeSetsSource = ResolveUsePrecomputedGenotypeSets(repeatSearchRequest.SearchRequest);

            var repeatSearchId = await repeatSearchDispatcher.DispatchSearch(repeatSearchRequest);
            LogPrecomputedGenotypeSetsChoice(
                repeatSearchRequest.SearchRequest, precomputedGenotypeSetsSource, repeatSearchRequest.OriginalSearchId, repeatSearchId);
            await repeatSearchDispatcher.DispatchSearchTrackingEvent(repeatSearchRequest, repeatSearchId);

            return new JsonResult(new SearchInitiationResponse
            {
                SearchIdentifier = repeatSearchRequest.OriginalSearchId,
                RepeatSearchIdentifier = repeatSearchId
            });
        }

        /// <summary>
        /// Resolves <see cref="SearchRequest.ParallelMatchPrediction"/> for the request. An explicit value supplied on
        /// the request is honoured as-is: <c>true</c> always takes the parallel ACA Worker ("Containers") path and
        /// <c>false</c> always takes the legacy sequential Durable path, regardless of the server-side controls.
        /// <para>
        /// Only when the request leaves the value <c>null</c> is it derived from the two server-side controls:
        /// <see cref="SearchFunctionSettings.DefaultParallelMatchPrediction"/> is the master switch — when <c>false</c>,
        /// the request takes the legacy sequential path, regardless of the percentage; when <c>true</c>,
        /// <see cref="SearchFunctionSettings.ParallelMatchPredictionRequestPercentage"/> percent of requests take the
        /// parallel path and the remainder fall back to the sequential path.
        /// </para>
        /// </summary>
        private void ResolveParallelMatchPrediction(SearchRequest searchRequest)
        {
            searchRequest.ParallelMatchPrediction ??=
                defaultParallelMatchPrediction
                && Random.Shared.Next(100) < parallelMatchPredictionRequestPercentage;
        }

        /// <summary>
        /// Resolves <see cref="SearchRequest.UsePrecomputedGenotypeSets"/> once for the search. A value named on the
        /// request is kept, in either direction. An empty value is chosen from the switch
        /// (<see cref="SearchFunctionSettings.DefaultUsePrecomputedGenotypeSets"/>) and the share
        /// (<see cref="SearchFunctionSettings.UsePrecomputedGenotypeSetsRequestPercentage"/>): when the switch is off, or
        /// the share is 0, the search calculates live.
        /// </summary>
        /// <remarks>
        /// Match prediction uses the resolved value and does not decide again, so a change of the settings affects new
        /// searches only.
        /// </remarks>
        private UsePrecomputedGenotypeSetsSource ResolveUsePrecomputedGenotypeSets(SearchRequest searchRequest)
        {
            if (searchRequest.UsePrecomputedGenotypeSets.HasValue)
            {
                return UsePrecomputedGenotypeSetsSource.Request;
            }

            searchRequest.UsePrecomputedGenotypeSets =
                defaultUsePrecomputedGenotypeSets
                && Random.Shared.Next(100) < usePrecomputedGenotypeSetsRequestPercentage;
            return UsePrecomputedGenotypeSetsSource.FeatureFlag;
        }

        /// <remarks>
        /// The source shows when a client names the value: production clients are expected to leave it empty.
        /// Logging must never fail the request.
        /// </remarks>
        private void LogPrecomputedGenotypeSetsChoice(
            SearchRequest searchRequest,
            UsePrecomputedGenotypeSetsSource source,
            string searchRequestId,
            string repeatSearchRequestId)
        {
            try
            {
                logger.SendEvent(PrecomputedGenotypeSetsChosenEventName, props: new Dictionary<string, string>
                {
                    { "SearchRequestId", searchRequestId },
                    { "RepeatSearchRequestId", repeatSearchRequestId },
                    { "UsePrecomputedGenotypeSets", searchRequest.UsePrecomputedGenotypeSets.ToString() },
                    { "UsePrecomputedGenotypeSetsSource", source.ToString() },
                });
            }
            catch
            {
                // The search is already dispatched; a lost event must not fail the request.
            }
        }

        private static IActionResult BuildValidationResponse(ValidationResult validationResult) =>
            new BadRequestObjectResult(validationResult.Errors);
    }
}