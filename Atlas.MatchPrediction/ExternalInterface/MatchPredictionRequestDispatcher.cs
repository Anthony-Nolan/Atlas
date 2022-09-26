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
        /// Dispatch a match prediction request for a single patient-donor pair without running a full search.
        /// Note: if the input is deemed invalid, then a <see cref="ValidationException"/> will be thrown.
        /// </summary>
        Task<MatchPredictionInitiationResponse> DispatchMatchPredictionRequest(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);

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

        public async Task<MatchPredictionInitiationResponse> DispatchMatchPredictionRequest(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            var requestId = await DispatchRequest(singleDonorMatchProbabilityInput);

            return new MatchPredictionInitiationResponse
            {
                MatchPredictionRequestId = requestId
            };
        }

        /// <inheritdoc />
        public async Task<BatchedMatchPredictionInitiationResponse> DispatchMatchPredictionRequestBatch(IEnumerable<SingleDonorMatchProbabilityInput> inputBatch)
        {
            var responses = new List<DonorResponse>();

            foreach (var input in inputBatch)
            {
                try
                {
                    var id = await DispatchRequest(input);
                    responses.Add(new DonorResponse
                    {
                        DonorId = GetDonorId(input),
                        MatchPredictionRequestId = id
                    });
                }
                catch (ValidationException ex)
                {
                    responses.Add(new DonorResponse
                    {
                        DonorId = GetDonorId(input),
                        ValidationErrors = ex.Errors.ToList()
                    });
                }
            }

            // Not using `.Single` here on purpose as don't want to throw in case multiple IDs (of same phenotype) were submitted.
            // Multiple IDs is an unlikely use case, and not worth fretting over.
            static int? GetDonorId(SingleDonorMatchProbabilityInput input) => input.Donor?.DonorIds?.FirstOrDefault();

            return new BatchedMatchPredictionInitiationResponse
            {
                DonorResponses = responses
            };
        }

        private async Task<string> DispatchRequest(SingleDonorMatchProbabilityInput input)
        {
            await new MatchProbabilityInputValidator().ValidateAndThrowAsync(input);

            var requestId = Guid.NewGuid().ToString();
            var request = new IdentifiedMatchPredictionRequest
            {
                SingleDonorMatchProbabilityInput = input,
                Id = requestId
            };

            await serviceBusClient.PublishToMatchPredictionRequestsTopic(request);

            return requestId;
        }
    }
}