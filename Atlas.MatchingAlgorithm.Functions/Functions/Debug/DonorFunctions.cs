using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Functions.Models.Debug;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Debug;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Azure.Core;
using Azure.Monitor.Query;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
{
    public class DonorFunctions
    {
        private readonly IDonorInspectionRepository inspectionRepository;
        private readonly IHlaExpansionFailuresService hlaExpansionFailuresService;
        private readonly IDonorUpdateRepository donorUpdateRepository;

        public DonorFunctions(IActiveRepositoryFactory activeRepositoryFactory, IHlaExpansionFailuresService hlaExpansionFailuresService)
        {
            inspectionRepository = activeRepositoryFactory.GetDonorInspectionRepository();
            this.hlaExpansionFailuresService = hlaExpansionFailuresService;
            donorUpdateRepository = activeRepositoryFactory.GetDonorUpdateRepository();
        }

        [FunctionName(nameof(GetAvailableDonorsFromActiveDb))]
        [ProducesResponseType(typeof(DebugDonorsResult), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableDonorsFromActiveDb(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/donors/active")]
            [RequestBodyType(typeof(string[]), "External Donor Codes")]
            HttpRequest request)
        {
            var externalDonorCodes = JsonConvert.DeserializeObject<string[]>(await new StreamReader(request.Body).ReadToEndAsync());
            var donors = await inspectionRepository.GetAvailableDonorsByExternalDonorCodes(externalDonorCodes);

            return new JsonResult(
                DebugDonorsHelper.BuildDebugDonorsResult(
                    externalDonorCodes, 
                    donors.Select(d => d.ToDonorDebugInfo()).ToList()
                    ));
        }

        [FunctionName(nameof(GetHlaExpansionFailures))]
        public async Task<IActionResult> GetHlaExpansionFailures(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(GetHlaExpansionFailures)}/" + "{daysToQuery?}"
                )]
            HttpRequest request,
            int? daysToQuery
        )
        {
            var output = await hlaExpansionFailuresService.Query(daysToQuery ?? 14);
            
            return new ContentResult
            {
                Content = JToken.FromObject(output).ToString(), // Serializing in human friendly way
                StatusCode = StatusCodes.Status200OK,
                ContentType = ContentType.ApplicationJson.ToString()
            };
        }

        [FunctionName(nameof(SetDonorsAsUnavailableForSearch))]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task SetDonorsAsUnavailableForSearch(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/donors/makeUnavailableForSearch")]
            [RequestBodyType(typeof(string[]), "External Donor Codes")]
            HttpRequest request)
        {
            var externalDonorCodes = await request.DeserialiseRequestBody<string[]>();
            var donors = await inspectionRepository.GetAvailableDonorsByExternalDonorCodes(externalDonorCodes);
            await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(donors.Select(d => d.DonorId));
        }
    }
}