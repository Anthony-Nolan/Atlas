using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.Common.Utils.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services.Debug;
using AzureFunctions.Extensions.Swashbuckle.Attribute;

namespace Atlas.DonorImport.Functions.Functions.Debug
{
    public class DonorImportFunctions
    {
        private const string RoutePrefix = $"{RouteConstants.DebugRoutePrefix}/donorImport/";

        private readonly IDonorImportBlobStorageClient donorImportBlobStorageClient;
        private readonly IDonorImportResultsPeeker resultsPeeker;

        public DonorImportFunctions(
            IDonorImportBlobStorageClient donorImportBlobStorageClient,
            IDonorImportResultsPeeker resultsPeeker)
        {
            this.donorImportBlobStorageClient = donorImportBlobStorageClient;
            this.resultsPeeker = resultsPeeker;
        }

        /// <summary>
        /// Debug endpoint to post a donor import file.
        /// This endpoint is intended for requests with a small number of records for debug and testing purposes only.
        /// </summary>
        [FunctionName(nameof(PostDonorImportFile))]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task PostDonorImportFile(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RoutePrefix}file")]
            [RequestBodyType(typeof(DonorImportRequest), nameof(DonorImportRequest))]
            HttpRequest request)
        {
            var importFile = await request.DeserialiseRequestBody<DonorImportRequest>();
            await donorImportBlobStorageClient.UploadFile(importFile.FileContents, importFile.FileName);
        }


        [FunctionName(nameof(PeekDonorImportResultsMessages))]
        [ProducesResponseType(typeof(PeekServiceBusMessagesResponse<DonorImportMessage>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekDonorImportResultsMessages(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RoutePrefix}results/")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var response = await resultsPeeker.PeekResultsMessages(peekRequest);
            return new JsonResult(response);
        }
    }
}