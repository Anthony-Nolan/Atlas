using System;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Clients;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Validators;
using FluentValidation;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionRequestDispatcher
    {
        /// <summary>
        /// Dispatch a match prediction request for a single patient-donor pair without running a full search.
        /// </summary>
        Task<string> DispatchMatchPredictionRequest(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
    }

    public class MatchPredictionRequestDispatcher : IMatchPredictionRequestDispatcher
    {
        private readonly IMatchPredictionBusClient serviceBusClient;

        public MatchPredictionRequestDispatcher(IMatchPredictionBusClient serviceBusClient)
        {
            this.serviceBusClient = serviceBusClient;
        }

        /// <returns>A unique identifier for the dispatched Match Prediction request</returns>
        public async Task<string> DispatchMatchPredictionRequest(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            await new MatchProbabilityInputValidator().ValidateAndThrowAsync(singleDonorMatchProbabilityInput);
            var requestId = Guid.NewGuid().ToString();

            var request = new IdentifiedMatchPredictionRequest
            {
                SingleDonorMatchProbabilityInput = singleDonorMatchProbabilityInput,
                Id = requestId
            };

            await serviceBusClient.PublishToMatchPredictionRequestsTopic(request);

            return requestId;
        }
    }
}