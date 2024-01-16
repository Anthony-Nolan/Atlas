using System.Collections.Generic;
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
using Atlas.Client.Models.Debug;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Functions.Models.Debug;
using Atlas.DonorImport.Models;
using Atlas.Common.Debugging;

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
        [ProducesResponseType(typeof(DebugDonorsResult<Donor>), (int)HttpStatusCode.OK)]
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

        [FunctionName(nameof(GetRandomDonors))]
        [ProducesResponseType(typeof(IEnumerable<Donor>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRandomDonors(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/donors/random")]
            [RequestBodyType(typeof(DonorRequest), "Donors to retrieve")]
            HttpRequest request)
        {
            var donorRequest = JsonConvert.DeserializeObject<DonorRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var donorType = donorRequest.DonorType?.ToDatabaseType();
            var donors = await donorReadRepository.Get1000RandomDonors(donorType, donorRequest.RegistryCode);

            return new JsonResult(donors);
        }
    }
}