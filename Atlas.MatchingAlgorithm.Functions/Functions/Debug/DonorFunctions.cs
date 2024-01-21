using Atlas.Common.Utils.Http;
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
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.Common.Debugging;
using Atlas.Debug.Client.Models.DonorImport;

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
        [ProducesResponseType(typeof(IEnumerable<DebugDonorsResult<Donor>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonorsFromActiveDb(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/donors/active")]
            [RequestBodyType(typeof(string[]), "External Donor Codes")]
            HttpRequest request)
        {
            var externalDonorCodes = JsonConvert.DeserializeObject<string[]>(await new StreamReader(request.Body).ReadToEndAsync());
            var donors = await inspectionRepository.GetDonorsByExternalDonorCodes(externalDonorCodes);

            return new JsonResult(
                DebugDonorsHelper.BuildDebugDonorsResult(
                    externalDonorCodes, 
                    donors.ToList(), 
                    d => d.ExternalDonorCode));
        }
    }
}