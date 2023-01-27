using Atlas.Common.Debugging.Donors;
using Atlas.Common.Utils.Http;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
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

namespace Atlas.DonorImport.Functions.Functions.Debug
{
    public class DonorFunctions
    {
        private readonly IDonorReader donorReader;

        public DonorFunctions(IDonorReader donorReader)
        {
            this.donorReader = donorReader;
        }

        [FunctionName(nameof(GetDonors))]
        [ProducesResponseType(typeof(DebugDonorsResult<Donor, string>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonors(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/donors")]
            [RequestBodyType(typeof(string[]), "Donor Record Ids")]
            HttpRequest request)
        {
            var recordIds = JsonConvert.DeserializeObject<string[]>(await new StreamReader(request.Body).ReadToEndAsync());

            var donors = await donorReader.GetDonorsByExternalDonorCodes(recordIds);

            return new JsonResult(
                DebugDonorsHelper.BuildDebugDonorsResult(recordIds, donors.Values.ToList(), d => d.ExternalDonorCode));
        }
    }
}