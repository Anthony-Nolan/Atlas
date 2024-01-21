using Atlas.Client.Models.Search.Results;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Http;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise4;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    /// <summary>
    /// Functions required for running of exercise 4 of the WMDA BioInformatics and Innovation Working Group.
    /// </summary>
    public class Exercise4Functions
    {
        private const string FunctionNamePrefix = "Exercise4_";

        private readonly IValidationAtlasPreparer atlasPreparer;
        private readonly ISearchRequester searchRequester;
        private readonly IResultSetProcessor<SearchResultsNotification> searchResultSetProcessor;
        private readonly ISearchResultNotificationSender messageSender;

        public Exercise4Functions(
            IValidationAtlasPreparer atlasPreparer,
            ISearchRequester searchRequester,
            IResultSetProcessor<SearchResultsNotification> searchResultSetProcessor,
            ISearchResultNotificationSender messageSender)
        {
            this.atlasPreparer = atlasPreparer;
            this.searchRequester = searchRequester;
            this.searchResultSetProcessor = searchResultSetProcessor;
            this.messageSender = messageSender;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName($"{FunctionNamePrefix}1_{nameof(PrepareAtlasDonorStores)}")]
        public async Task PrepareAtlasDonorStores(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            try
            {
                await atlasPreparer.PrepareAtlasWithImportedDonors();
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to prepare Atlas donor stores.", ex);
            }
        }

        [FunctionName($"{FunctionNamePrefix}{nameof(HandleDataRefreshCompletion)}")]
        public async Task HandleDataRefreshCompletion(
        [ServiceBusTrigger(
                "%DataRefresh:CompletionTopic%",
                "%DataRefresh:CompletionTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            CompletedDataRefresh dataRefresh)
        {
            await atlasPreparer.SaveDataRefreshDetails(dataRefresh);
        }

        [FunctionName($"{FunctionNamePrefix}2_{nameof(SendSearchRequests)}")]
        public async Task<IActionResult> SendSearchRequests(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        [RequestBodyType(typeof(ValidationSearchRequest), nameof(ValidationSearchRequest))]
        HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<ValidationSearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var searchSetId = await searchRequester.SubmitSearchRequests(searchRequest);

            return new OkObjectResult(searchSetId);
        }

        [FunctionName($"{FunctionNamePrefix}{nameof(FetchSearchResults)}")]
        public async Task FetchSearchResults(
            [ServiceBusTrigger(
                "%Search:ResultsTopic%",
                "%Search:ResultsTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            SearchResultsNotification notification)
        {
            try
            {
                await searchResultSetProcessor.ProcessAndStoreResultSet(notification);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while downloading results for {notification.SearchRequestId}: {ex.GetBaseException()}");
                throw;
            }
        }

        [FunctionName($"{FunctionNamePrefix}{nameof(ManuallySendSuccessNotificationForSearches)}")]
        public async Task ManuallySendSuccessNotificationForSearches(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(string[]), "searchRequestIds")]
            HttpRequest request)
        {
            try
            {
                var ids = JsonConvert.DeserializeObject<string[]>(await new StreamReader(request.Body).ReadToEndAsync());
                await messageSender.SendSuccessNotifications(ids);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failure whilst downloading results.", ex);
            }
        }
    }
}