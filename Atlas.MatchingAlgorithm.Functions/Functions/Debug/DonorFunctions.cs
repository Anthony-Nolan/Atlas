using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Atlas.Common.Debugging.Donors;

namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
{
    public class DonorFunctions
    {
        private readonly IDonorInspectionRepository inspectionRepository;

        public DonorFunctions(IActiveRepositoryFactory activeRepositoryFactory)
        {
            inspectionRepository = activeRepositoryFactory.GetDonorInspectionRepository();
        }

        [FunctionName(nameof(GetDonorsFromActiveDb))]
        [ProducesResponseType(typeof(IEnumerable<DebugDonorsResult<DonorInfo, int>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonorsFromActiveDb(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/donors/active")]
            [RequestBodyType(typeof(int[]), "Atlas Donor Ids")]
            HttpRequest request)
        {
            var donorIds = JsonConvert.DeserializeObject<int[]>(await new StreamReader(request.Body).ReadToEndAsync());

            var donors = await inspectionRepository.GetDonors(donorIds);

            return new JsonResult(
                DebugDonorsHelper.BuildDebugDonorsResult(donorIds, donors.Values.ToList(), d => d.DonorId));
        }
    }
}