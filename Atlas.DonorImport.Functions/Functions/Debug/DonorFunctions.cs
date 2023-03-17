using Atlas.Common.Debugging.Donors;
using Atlas.Common.Utils.Http;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;

namespace Atlas.DonorImport.Functions.Functions.Debug
{
    public class DonorFunctions
    {
        private readonly IDonorReadRepository donorReadRepository;

        public DonorFunctions(IDonorReadRepository donorReadRepository)
        {
            this.donorReadRepository = donorReadRepository;
        }

        [FunctionName(nameof(GetDonors))]
        [ProducesResponseType(typeof(DebugDonorsResult<Data.Models.Donor, string>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonors(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/donors")]
            [RequestBodyType(typeof(string[]), "Donor Record Ids")]
            HttpRequest request)
        {
            var recordIds = JsonConvert.DeserializeObject<string[]>(await new StreamReader(request.Body).ReadToEndAsync());

            var donors = await donorReadRepository.GetDonorsByExternalDonorCodes(recordIds);

            return new JsonResult(
                DebugDonorsHelper.BuildDebugDonorsResult(recordIds, donors.Values.ToList(), d => d.ExternalDonorCode));
        }
    }
}