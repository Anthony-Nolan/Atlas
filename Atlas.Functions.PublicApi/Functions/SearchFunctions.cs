using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
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
        private readonly IMatchPredictionAlgorithmValidator matchPredictionAlgorithmValidator;

        public SearchFunctions(ISearchDispatcher searchDispatcher, IMatchPredictionAlgorithmValidator matchPredictionAlgorithmValidator)
        {
            this.searchDispatcher = searchDispatcher;
            this.matchPredictionAlgorithmValidator = matchPredictionAlgorithmValidator;
        }

        [FunctionName(nameof(Search))]
        public async Task<IActionResult> Search(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            var matchingValidationResult = await new SearchRequestValidator().ValidateAsync(searchRequest);
            if (!matchingValidationResult.IsValid)
            {
                return BuildValidationResponse(matchingValidationResult);
            }

            var probabilityRequestToValidate = searchRequest.ToPartialMatchProbabilitySearchRequest();
            var probabilityValidationResult = matchPredictionAlgorithmValidator.ValidateMatchPredictionAlgorithmInput(probabilityRequestToValidate);
            if (!probabilityValidationResult.IsValid)
            {
                return BuildValidationResponse(probabilityValidationResult);
            }

            var id = await searchDispatcher.DispatchSearch(searchRequest);
            return new JsonResult(new SearchInitiationResponse {SearchIdentifier = id});
        }

        private static IActionResult BuildValidationResponse(ValidationResult validationResult) =>
            new BadRequestObjectResult(validationResult.Errors);
    }
}