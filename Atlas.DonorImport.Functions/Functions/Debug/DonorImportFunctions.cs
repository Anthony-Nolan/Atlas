using Atlas.Client.Models.Debug;
using Atlas.Common.Utils.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services.Debug;
using AzureFunctions.Extensions.Swashbuckle.Attribute;

namespace Atlas.DonorImport.Functions.Functions.Debug
{
    public class DonorImportFunctions
    {
        private readonly IDonorImportResultsPeeker resultsPeeker;

        public DonorImportFunctions(IDonorImportResultsPeeker resultsPeeker)
        {
            this.resultsPeeker = resultsPeeker;
        }

        [FunctionName(nameof(PeekDonorImportResultsMessages))]
        [ProducesResponseType(typeof(IEnumerable<DonorImportMessage>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekDonorImportResultsMessages(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RouteConstants.DebugRoutePrefix}/donorImport/results/")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var messages = await resultsPeeker.PeekResultsMessages(peekRequest);
            return new JsonResult(messages);
        }
    }
}