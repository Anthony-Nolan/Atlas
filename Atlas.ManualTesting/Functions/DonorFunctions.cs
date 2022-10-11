using System.Threading.Tasks;
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

        public DonorFunctions(ISearchableDonorUpdatesPeeker searchableDonorUpdatesPeeker)
        {
            this.searchableDonorUpdatesPeeker = searchableDonorUpdatesPeeker;
        }

        [FunctionName(nameof(FilterSearchableDonorUpdatesByAtlasDonorId))]
        public async Task<IActionResult> FilterSearchableDonorUpdatesByAtlasDonorId(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekByAtlasDonorIdRequest), nameof(PeekByAtlasDonorIdRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekByAtlasDonorIdRequest>();

            var resultsNotifications = await searchableDonorUpdatesPeeker.GetMessagesByAtlasDonorId(peekRequest);

            return new JsonResult(resultsNotifications);
        }
    }
}