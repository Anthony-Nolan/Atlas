using System;
using Atlas.Common.Utils.Http;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.Exceptions;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.ManualTesting.Functions
{
    /// <summary>
    /// Set of functions to help test the `Atlas.Debug.Client`.
    /// Not every debug endpoint should be included here.
    /// The intended usage is that method contents can be overridden as needed when testing locally.
    /// </summary>
    public class DebugClientFunctions
    {
        private readonly IDonorImportFunctionsClient donorImportClient;
        private readonly IMatchingAlgorithmFunctionsClient matchingClient;
        private readonly ITopLevelFunctionsClient topLevelClient;
        private readonly IPublicApiFunctionsClient publicApiClient;
        private readonly IRepeatSearchFunctionsClient repeatSearchClient;

        public DebugClientFunctions(
            IDonorImportFunctionsClient donorImportClient, 
            ITopLevelFunctionsClient topLevelClient, 
            IMatchingAlgorithmFunctionsClient matchingClient,
            IPublicApiFunctionsClient publicApiClient,
            IRepeatSearchFunctionsClient repeatSearchClient)
        {
            this.donorImportClient = donorImportClient;
            this.topLevelClient = topLevelClient;
            this.matchingClient = matchingClient;
            this.publicApiClient = publicApiClient;
            this.repeatSearchClient = repeatSearchClient;
        }

        [Function(nameof(TestDonorImportDebugClientTest))]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<PeekServiceBusMessagesResponse<DonorImportMessage>> TestDonorImportDebugClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            return await donorImportClient.PeekDonorImportResultMessages(new PeekServiceBusMessagesRequest
            {
                MessageCount = 10
            });
        }

        [Function(nameof(TestTopLevelDebugClientTest))]
        public async Task<SearchResultSet> TestTopLevelDebugClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            return await topLevelClient.FetchSearchResultSet(new DebugSearchResultsRequest
            {
                SearchResultBlobContainer = "override-this",
                SearchResultFileName = "override-this.json",
                BatchFolderName = "override-this"
            });
        }

        [Function(nameof(TestMatchingAlgorithmDebugClientTest))]
        public async Task<OriginalMatchingAlgorithmResultSet> TestMatchingAlgorithmDebugClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            return await matchingClient.FetchMatchingResultSet(new DebugSearchResultsRequest
            {
                SearchResultFileName = "override-this.json"
            });
        }

        [Function(nameof(TestPublicApiDebugClientTest))]
        public async Task<IActionResult> TestPublicApiDebugClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(RepeatSearchRequest), "Search Request")]
            HttpRequest request)
        {
            try
            {
                var searchRequest = await request.DeserialiseRequestBody<RepeatSearchRequest>();
                return new JsonResult(await publicApiClient.PostRepeatSearchRequest(searchRequest));
            }
            catch (HttpFunctionException ex)
            {
                return new ObjectResult(ex) { StatusCode = (int)ex.HttpStatusCode };
            }
            catch (Exception)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(TestRepeatSearchDebugClientTest))]
        public async Task<IActionResult> TestRepeatSearchDebugClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            return new JsonResult(await repeatSearchClient.PeekMatchingResultNotifications(new PeekServiceBusMessagesRequest()
            {
                MessageCount = 10
            }));
        }

        [Function(nameof(HealthCheckTest))]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<string> HealthCheckTest(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            return await donorImportClient.HealthCheck();
        }
    }
}