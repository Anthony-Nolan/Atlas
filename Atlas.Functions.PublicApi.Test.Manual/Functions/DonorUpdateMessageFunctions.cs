using System.Threading.Tasks;
using Atlas.Functions.PublicApi.Test.Manual.Helpers;
using Atlas.Functions.PublicApi.Test.Manual.Models;
using Atlas.Functions.PublicApi.Test.Manual.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace Atlas.Functions.PublicApi.Test.Manual.Functions
{
    public class DonorUpdateMessageFunctions
    {
        private readonly ISearchableDonorUpdatesPeeker searchableDonorUpdatesPeeker;

        public DonorUpdateMessageFunctions(ISearchableDonorUpdatesPeeker searchableDonorUpdatesPeeker)
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