﻿using Atlas.Common.Utils;
using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise3;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Validation.Functions
{
    /// <summary>
    /// Functions required for running of exercise 3 of the WMDA consensus dataset.
    /// </summary>
    public class Exercise3Functions
    {
        private const string FunctionNamePrefix = "Exercise3_";
        private readonly IMatchPredictionRequester matchPredictionRequester;
        private readonly IMatchPredictionResultsProcessor matchPredictionResultsProcessor;
        private readonly IMatchPredictionLocationSender matchPredictionLocationSender;

        public Exercise3Functions(
            IMatchPredictionRequester matchPredictionRequester, 
            IMatchPredictionResultsProcessor matchPredictionResultsProcessor, 
            IMatchPredictionLocationSender matchPredictionLocationSender)
        {
            this.matchPredictionRequester = matchPredictionRequester;
            this.matchPredictionResultsProcessor = matchPredictionResultsProcessor;
            this.matchPredictionLocationSender = matchPredictionLocationSender;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function($"{FunctionNamePrefix}{nameof(SendMatchPredictionRequests)}")]
        public async Task SendMatchPredictionRequests(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            try
            {
                await matchPredictionRequester.SendMatchPredictionRequests();
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failure whilst sending requests.", ex);
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function($"{FunctionNamePrefix}{nameof(ResumeMatchPredictionRequests)}")]
        public async Task ResumeMatchPredictionRequests(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            try
            {
                await matchPredictionRequester.ResumeMatchPredictionRequests();
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failure whilst resuming to send requests.", ex);
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function($"{FunctionNamePrefix}{nameof(PromptDownloadOfMissingResults)}")]
        public async Task PromptDownloadOfMissingResults(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            try
            {
                await matchPredictionLocationSender.PublishLocationsForMatchPredictionRequestMissingResults();
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failure whilst downloading missing results.", ex);
            }
        }

        [Function($"{FunctionNamePrefix}{nameof(ProcessResults)}")]
        public async Task ProcessResults(
            [ServiceBusTrigger(
                "%MatchPredictionRequests:ResultsTopic%",
                "%MatchPredictionRequests:ResultsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            MatchPredictionResultLocation[] locations)
        {
            try
            {
                await matchPredictionResultsProcessor.ProcessAndStoreResults(locations);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to process results.", ex);
            }
        }
    }
}