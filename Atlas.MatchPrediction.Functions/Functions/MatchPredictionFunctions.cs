using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Functions.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using FluentValidation;
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

            try
            {
                var input = matchPredictionRequest.ToSingleDonorMatchProbabilityInput();
                var response = await requestDispatcher.DispatchMatchPredictionRequest(input);
                return new JsonResult(response);
            }
            catch (ValidationException ex)
            {
                return new BadRequestObjectResult(ex.Errors);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        /// <summary>
        /// Submits a batch of match prediction requests - one patient vs. a set of donors - without running a full search.
        /// </summary>
        /// <returns>Set of match prediction request IDs.</returns>
        [FunctionName(nameof(BatchMatchPredictionRequests))]
        public async Task<IActionResult> BatchMatchPredictionRequests(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(BatchedMatchPredictionRequests), nameof(BatchMatchPredictionRequests))]
            HttpRequest request)
        {
            var batch = JsonConvert.DeserializeObject<BatchedMatchPredictionRequests>(await new StreamReader(request.Body).ReadToEndAsync());

            try
            {
                var inputs = batch.ToSingleDonorMatchProbabilityInputs().ToList();

                if (inputs.IsNullOrEmpty())
                {
                    return new BadRequestObjectResult("No match probability inputs submitted.");
                }

                // every input in the batch will have the same non-donor info, and so only one input need be validated
                var results = matchPredictionValidator.ValidateMatchProbabilityNonDonorInput(inputs.First());
                if (!results.IsValid)
                {
                    return new BadRequestObjectResult(results.Errors);
                }

                var response = await requestDispatcher.DispatchMatchPredictionRequestBatch(inputs);
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
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