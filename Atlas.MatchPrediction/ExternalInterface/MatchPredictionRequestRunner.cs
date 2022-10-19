using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Common.Validation;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Exceptions;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using FluentValidation;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionRequestRunner
    {
        /// <summary>
        /// Note on exception handling:
        /// - Individual requests that fail validation or contain invalid HLA will not disrupt the processing of the remaining requests in the batch;
        ///   details of invalid requests will be logged.
        /// - Any other exception will be allowed to throw, thus disrupting the entire batch.
        /// </summary>
        Task RunMatchPredictionRequestBatch(IEnumerable<IdentifiedMatchPredictionRequest> requestBatch);
    }

    public class MatchPredictionRequestRunner : IMatchPredictionRequestRunner
    {
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;
        private readonly IMatchPredictionRequestResultUploader resultUploader;
        private readonly IMessageBatchPublisher<MatchPredictionResultLocation> messagePublisher;
        private readonly ILogger logger;
        private readonly MatchPredictionRequestLoggingContext loggingContext;

        public MatchPredictionRequestRunner(
            IMatchPredictionAlgorithm matchPredictionAlgorithm, 
            IMatchPredictionRequestResultUploader resultUploader,
            IMessageBatchPublisher<MatchPredictionResultLocation> messagePublisher,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchPredictionRequestLoggingContext> logger,
            MatchPredictionRequestLoggingContext loggingContext)
        {
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.resultUploader = resultUploader;
            this.messagePublisher = messagePublisher;
            this.logger = logger;
            this.loggingContext = loggingContext;
        }

        public async Task RunMatchPredictionRequestBatch(IEnumerable<IdentifiedMatchPredictionRequest> requestBatch)
        {
            var resultsLocations = new List<MatchPredictionResultLocation>();

            foreach (var request in requestBatch)
            {
                try
                {
                    if (request == null)
                    {
                        continue;
                    }

                    resultsLocations.Add(await RunMatchPredictionRequest(request));
                }
                catch (ValidationException ex)
                {
                    logger.SendTrace("Invalid match prediction request", LogLevel.Error, new Dictionary<string, string>
                    {
                        {"ValidationErrors", ex.ToErrorMessagesString()}
                    });
                }
                catch (HlaMetadataDictionaryException ex)
                {
                    logger.SendTrace("Invalid HLA in match prediction request", LogLevel.Error, new Dictionary<string, string>
                    {
                        {"Locus", ex.Locus},
                        {"HlaName", ex.HlaName},
                        {"Exception", ex.ToString()}
                    });
                }
                catch (Exception ex)
                {
                    throw new MatchPredictionRequestException(ex);
                }
            }

            await messagePublisher.BatchPublish(resultsLocations);
        }

        private async Task<MatchPredictionResultLocation> RunMatchPredictionRequest(IdentifiedMatchPredictionRequest request)
        {
            loggingContext.Initialise(request);

            logger.SendTrace("Run match prediction request");
            var result = await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(request.SingleDonorMatchProbabilityInput);

            return await resultUploader.UploadMatchPredictionRequestResult(request.Id, result);
        }
    }
}