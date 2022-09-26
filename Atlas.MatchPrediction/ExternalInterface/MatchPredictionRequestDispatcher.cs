using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Dispatch a match prediction request for each patient-donor pair within the submitted batch, without running a full search.
        /// Note: if any single donor input is deemed invalid, then the validation errors will be logged within the response,
        /// and the <see cref="ValidationException"/> will be suppressed to allow processing of the batch to continue.
        /// Any other exception, e.g., a connection error, will be allowed to throw, thus disrupting the batch.
        /// </summary>
        Task<BatchedMatchPredictionInitiationResponse> DispatchMatchPredictionRequestBatch(IEnumerable<SingleDonorMatchProbabilityInput> inputBatch);
    }

    public class MatchPredictionRequestDispatcher : IMatchPredictionRequestDispatcher
    {
        private readonly IMatchPredictionBusClient serviceBusClient;

        public MatchPredictionRequestDispatcher(IMatchPredictionBusClient serviceBusClient)
        {
            this.serviceBusClient = serviceBusClient;
        }

        /// <inheritdoc />
        public async Task<BatchedMatchPredictionInitiationResponse> DispatchMatchPredictionRequestBatch(IEnumerable<SingleDonorMatchProbabilityInput> inputBatch)
        {
            var allResponses = new List<DonorResponse>();
            var validRequests = new List<IdentifiedMatchPredictionRequest>();

            foreach (var input in inputBatch)
            {
                try
                {
                    var request = await ValidateAndConvertToRequest(input);
                    validRequests.Add(request);
                    allResponses.Add(new DonorResponse
                    {
                        DonorId = GetDonorId(input),
                        MatchPredictionRequestId = request.Id
                    });
                }
                catch (ValidationException ex)
                {
                    allResponses.Add(new DonorResponse
                    {
                        DonorId = GetDonorId(input),
                        ValidationErrors = ex.Errors.ToList()
                    });
                }
            }

            await serviceBusClient.BatchPublishToMatchPredictionRequestsTopic(validRequests);

            // Not using `.Single` here on purpose as don't want to throw in case multiple IDs (of same phenotype) were submitted.
            // Multiple IDs is an unlikely use case, and not worth fretting over.
            static int? GetDonorId(SingleDonorMatchProbabilityInput input) => input.Donor?.DonorIds?.FirstOrDefault();

            return new BatchedMatchPredictionInitiationResponse
            {
                DonorResponses = allResponses
            };
        }

        private static async Task<IdentifiedMatchPredictionRequest> ValidateAndConvertToRequest(SingleDonorMatchProbabilityInput input)
        {
            await new MatchProbabilityInputValidator().ValidateAndThrowAsync(input);

            var requestId = Guid.NewGuid().ToString();
            return new IdentifiedMatchPredictionRequest
            {
                SingleDonorMatchProbabilityInput = input,
                Id = requestId
            };
        }
    }
}