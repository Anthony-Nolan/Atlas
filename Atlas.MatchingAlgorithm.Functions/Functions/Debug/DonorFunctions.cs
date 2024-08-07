﻿using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.ApplicationInsights;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Functions.Models.Debug;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Debug;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        [Function(nameof(GetAvailableDonorsFromActiveDb))]
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

        [Function(nameof(HlaExpansionFailures))]
        [ProducesResponseType(typeof(IEnumerable<HlaExpansionFailure>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> HlaExpansionFailures(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(HlaExpansionFailures)}/" + "{daysToQuery?}"
                )]
            HttpRequest request,
            int? daysToQuery
        )
        {
            var output = await hlaExpansionFailuresService.Query(daysToQuery ?? 14);

            return new JsonResult(output);
        }

        [Function(nameof(SetDonorsAsUnavailableForSearch))]
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