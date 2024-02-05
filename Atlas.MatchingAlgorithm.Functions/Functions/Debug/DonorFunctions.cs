using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Functions.Models.Debug;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
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
        [ProducesResponseType(typeof(DebugDonorsResult), (int)HttpStatusCode.OK)]
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
                    donors.Select(d => d.ToDonorDebugInfo()).ToList()
                    ));
        }
    }
}