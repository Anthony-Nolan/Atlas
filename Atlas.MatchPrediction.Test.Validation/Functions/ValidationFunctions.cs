using Atlas.Common.Utils;
using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Validation.Functions
{
    public class ValidationFunctions
    {
        private readonly ISubjectInfoImporter subjectInfoImporter;
        private readonly IMatchPredictionRequester matchPredictionRequester;
        private readonly IResultsProcessor resultsProcessor;
        private readonly IMessageSender messageSender;

        public ValidationFunctions(
            ISubjectInfoImporter subjectInfoImporter, 
            IMatchPredictionRequester matchPredictionRequester, 
            IResultsProcessor resultsProcessor, 
            IMessageSender messageSender)
        {
            this.subjectInfoImporter = subjectInfoImporter;
            this.matchPredictionRequester = matchPredictionRequester;
            this.resultsProcessor = resultsProcessor;
            this.messageSender = messageSender;
        }

        [FunctionName(nameof(ImportSubjects))]
        public async Task ImportSubjects(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(ImportRequest), nameof(ImportRequest))]
            HttpRequest request)
        {
            try
            {
                var importRequest = JsonConvert.DeserializeObject<ImportRequest>(await new StreamReader(request.Body).ReadToEndAsync());
                await subjectInfoImporter.Import(importRequest);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to import subjects.", ex);
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(SendMatchPredictionRequests))]
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
        [FunctionName(nameof(ResumeMatchPredictionRequests))]
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
        [FunctionName(nameof(PromptDownloadOfMissingResults))]
        public async Task PromptDownloadOfMissingResults(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            try
            {
                await messageSender.SendNotificationsForMissingResults();
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failure whilst downloading missing results.", ex);
            }
        }

        [FunctionName(nameof(ProcessResults))]
        public async Task ProcessResults(
            [ServiceBusTrigger(
                "%MatchPredictionRequests:ResultsTopic%",
                "%MatchPredictionRequests:ResultsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            MatchPredictionResultLocation[] locations)
        {
            try
            {
                await resultsProcessor.ProcessAndStoreResults(locations);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to process results.", ex);
            }
        }
    }
}