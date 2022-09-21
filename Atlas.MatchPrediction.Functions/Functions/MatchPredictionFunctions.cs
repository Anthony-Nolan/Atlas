using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Functions.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class MatchPredictionFunctions
    {
        private readonly IMatchPredictionValidator matchPredictionValidator;
        private readonly IMatchPredictionRequestDispatcher requestDispatcher;
        private readonly IMatchPredictionRequestRunner requestRunner;

        public MatchPredictionFunctions(
            IMatchPredictionValidator matchPredictionValidator,
            IMatchPredictionRequestDispatcher requestDispatcher,
            IMatchPredictionRequestRunner requestRunner)
        {
            this.matchPredictionValidator = matchPredictionValidator;
            this.requestDispatcher = requestDispatcher;
            this.requestRunner = requestRunner;
        }

        /// <summary>
        /// Submits a match prediction request for a single patient-donor pair without running a full search.
        /// </summary>
        /// <returns>Unique Id for the match prediction request.</returns>
        [FunctionName(nameof(RequestMatchPrediction))]
        public async Task<IActionResult> RequestMatchPrediction(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(MatchPredictionRequest), nameof(MatchPredictionRequest))]
            HttpRequest request)
        {
            var matchPredictionRequest = JsonConvert.DeserializeObject<MatchPredictionRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var input = matchPredictionRequest.ToSingleDonorMatchProbabilityInput();

            var validationResult = matchPredictionValidator.ValidateMatchProbabilityInput(input);
            if (!validationResult.IsValid)
            {
                return new BadRequestObjectResult(validationResult.Errors);
            }

            var id = await requestDispatcher.DispatchMatchPredictionRequest(input);
            return new JsonResult(new MatchPredictionInitiationResponse { MatchPredictionRequestId = id });
        }

        [FunctionName(nameof(RunMatchPredictionRequestBatch))]
        public async Task RunMatchPredictionRequestBatch(
            [ServiceBusTrigger(
                "%MatchPredictionRequests:ServiceBusTopic%",
                "%MatchPredictionRequests:ServiceBusSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            IdentifiedMatchPredictionRequest[] requestBatch)
        {
            await requestRunner.RunMatchPredictionRequestBatch(requestBatch);
        }
    }
}