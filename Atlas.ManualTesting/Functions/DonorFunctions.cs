using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.ManualTesting.Helpers;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.ManualTesting.Functions
{
    public class DonorFunctions
    {
        private readonly ISearchableDonorUpdatesPeeker searchableDonorUpdatesPeeker;
        private readonly IDonorStoresInspector donorStoresInspector;

        public DonorFunctions(ISearchableDonorUpdatesPeeker searchableDonorUpdatesPeeker, IDonorStoresInspector donorStoresInspector)
        {
            this.searchableDonorUpdatesPeeker = searchableDonorUpdatesPeeker;
            this.donorStoresInspector = donorStoresInspector;
        }

        [FunctionName(nameof(FilterSearchableDonorUpdatesByAtlasDonorIds))]
        public async Task<IActionResult> FilterSearchableDonorUpdatesByAtlasDonorIds(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekByAtlasDonorIdsRequest), nameof(PeekByAtlasDonorIdsRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekByAtlasDonorIdsRequest>();

            var resultsNotifications = await searchableDonorUpdatesPeeker.GetMessagesByAtlasDonorId(peekRequest);

            return new JsonResult(resultsNotifications);
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(GetDonorsMissingFromActiveMatchingDatabase))]
        public async Task<IActionResult> GetDonorsMissingFromActiveMatchingDatabase(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var missingIds = await donorStoresInspector.GetDonorsMissingFromActiveMatchingDatabase();

            return new JsonResult(missingIds);
        }
    }
}