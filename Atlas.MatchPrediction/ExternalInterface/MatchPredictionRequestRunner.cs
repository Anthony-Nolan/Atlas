using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.ServiceBus.Models;
using Atlas.Common.Validation;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Exceptions;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using FluentValidation;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public class MatchPredictionRequestProcessor : MessageProcessor<IdentifiedMatchPredictionRequest>
    {
        public MatchPredictionRequestProcessor(IServiceBusMessageReceiver<IdentifiedMatchPredictionRequest> messageReceiver) : base(messageReceiver)
        {
        }
    }

    public interface IMatchPredictionRequestRunner
    {
        Task RunMatchPredictionRequestBatch();
    }

    public class MatchPredictionRequestRunner : IMatchPredictionRequestRunner
    {
        private readonly IMessageProcessor<IdentifiedMatchPredictionRequest> messageProcessor;
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;
        private readonly IMatchPredictionRequestResultUploader resultUploader;
        private readonly ILogger logger;
        private readonly MatchPredictionRequestLoggingContext loggingContext;
        private readonly int batchSize;

        public MatchPredictionRequestRunner(
            IMessageProcessor<IdentifiedMatchPredictionRequest> messageProcessor,
            IMatchPredictionAlgorithm matchPredictionAlgorithm, 
            IMatchPredictionRequestResultUploader resultUploader,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchPredictionRequestLoggingContext> logger,
            MatchPredictionRequestLoggingContext loggingContext,
            MatchPredictionRequestsSettings settings)
        {
            this.messageProcessor = messageProcessor;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.resultUploader = resultUploader;
            this.logger = logger;
            this.loggingContext = loggingContext;
            batchSize = settings.BatchSize;
        }

        public async Task RunMatchPredictionRequestBatch()
        {
            await messageProcessor.ProcessAllMessagesInBatches_Async(
                async batch => await ProcessMatchPredictionRequests(batch),
                batchSize,
                batchSize * 2
                );
        }

        private async Task ProcessMatchPredictionRequests(IEnumerable<ServiceBusMessage<IdentifiedMatchPredictionRequest>> messageBatch)
        {
            foreach (var message in messageBatch)
            {
                try
                {
                    var request = message.DeserializedBody;

                    if (request == null)
                    {
                        continue;
                    }

                    loggingContext.Initialise(request);

                    logger.SendTrace("Run match prediction request");
                    var result = await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(request.SingleDonorMatchProbabilityInput);

                    await resultUploader.UploadMatchPredictionRequestResult(request.Id, result);
                    logger.SendTrace("Match prediction request completed & results uploaded");
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
        }
    }
}