using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils.Concurrency;
using Atlas.Common.Validation;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Exceptions;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.ExternalInterface;

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
    private const string BatchTimingMessage = "Run Match Prediction Request Batch";
    private const string RequestTimingMessage = "Run Match Prediction Request";

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IMessageBatchPublisher<MatchPredictionResultLocation> messagePublisher;
    private int maxDegreeOfParallelism;
    private readonly ILogger serviceLogger;

    public MatchPredictionRequestRunner(
        IServiceScopeFactory serviceScopeFactory,
        IMessageBatchPublisher<MatchPredictionResultLocation> messagePublisher,
        MatchPredictionRequestsSettings settings,
        ILogger serviceLogger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.messagePublisher = messagePublisher;
        maxDegreeOfParallelism = settings.MaxParallelism;
        this.serviceLogger = serviceLogger;
    }

    public async Task RunMatchPredictionRequestBatch(IEnumerable<IdentifiedMatchPredictionRequest> requestBatch)
    {
        var requests = requestBatch
            .Where(request => request != null)
            .ToList();

        using (serviceLogger.RunTimed(BatchTimingMessage))
        {
            var resultsLocations = await requests.WhenAll(RunMatchPredictionRequest, maxDegreeOfParallelism);

            await messagePublisher.BatchPublish(resultsLocations.Where(location => location != null));
        }
    }

    private async Task<MatchPredictionResultLocation> RunMatchPredictionRequest(IdentifiedMatchPredictionRequest request)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var loggingContext = serviceProvider.GetRequiredService<MatchPredictionRequestLoggingContext>();
        var requestLogger = serviceProvider.GetRequiredService<IMatchPredictionLogger<MatchPredictionRequestLoggingContext>>();
        var matchPredictionAlgorithm = serviceProvider.GetRequiredService<IMatchPredictionAlgorithm>();
        var resultUploader = serviceProvider.GetRequiredService<IMatchPredictionRequestResultUploader>();

        loggingContext.Initialise(request);

        using (requestLogger.RunTimed(RequestTimingMessage))
        {
            try
            {
                var result = await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(request.SingleDonorMatchProbabilityInput);

                return await resultUploader.UploadMatchPredictionRequestResult(request.Id, result);
            }
            catch (ValidationException ex)
            {
                requestLogger.SendTrace("Invalid match prediction request", LogLevel.Error, new Dictionary<string, string>
                    {
                        { "ValidationErrors", ex.ToErrorMessagesString() }
                    }
                );
                return null;
            }
            catch (HlaMetadataDictionaryException ex)
            {
                requestLogger.SendTrace("Invalid HLA in match prediction request", LogLevel.Error, new Dictionary<string, string>
                    {
                        { "Locus", ex.Locus },
                        { "HlaName", ex.HlaName },
                        { "Exception", ex.ToString() }
                    }
                );
                return null;
            }
            catch (Exception ex)
            {
                throw new MatchPredictionRequestException(ex);
            }
        }
    }
}