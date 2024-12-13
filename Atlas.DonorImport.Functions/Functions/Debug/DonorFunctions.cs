using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Functions.Models.Debug;
using Atlas.DonorImport.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Functions.Functions.Debug
{
    public class DonorFunctions
    {
        private const string RoutePrefix = $"{RouteConstants.DebugRoutePrefix}/donors/";
        private readonly IDonorReadRepository donorReadRepository;
        private readonly IDonorImportRepository donorImportRepository;

        public DonorFunctions(
            IDonorReadRepository donorReadRepository,
            IDonorImportRepository donorImportRepository)
        {
            this.donorReadRepository = donorReadRepository;
            this.donorImportRepository = donorImportRepository;
        }

        [FunctionName(nameof(GetDonors))]
        [ProducesResponseType(typeof(DebugDonorsResult), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonors(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RoutePrefix}")]
            [RequestBodyType(typeof(string[]), "Donor Record Ids")]
            HttpRequest request)
        {
            var recordIds = JsonConvert.DeserializeObject<string[]>(await new StreamReader(request.Body).ReadToEndAsync());
            var donors = await donorReadRepository.GetDonorsByExternalDonorCodes(recordIds);

            return new JsonResult(DebugDonorsHelper.BuildDebugDonorsResult(
                recordIds,
                donors.Values.Select(d => d.ToDonorDebugInfo()).ToList()
                ));
        }

        /// <summary>
        /// <paramref name="updatedBeforeDate"/> must be encoded as "yyyyMMdd".
        /// It is a non-inclusive filter - only donors updated before the given date will be returned.
        /// </summary>
        [FunctionName(nameof(GetDonorCodesByRegistry))]
        [ProducesResponseType(typeof(IEnumerable<string>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonorCodesByRegistry(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = RoutePrefix + "{registryCode}/externalDonorCodes/{updatedBeforeDate?}")]
            HttpRequest request,
            string registryCode,
            string updatedBeforeDate)
        {
            var lastUpdatedBeforeDate = DateTime.ParseExact(updatedBeforeDate, "yyyyMMdd", CultureInfo.InvariantCulture);
            var donors = await donorReadRepository.GetExternalDonorCodesLastUpdatedBefore(registryCode,
                DatabaseDonorType.Adult, lastUpdatedBeforeDate);
            var cords = await donorReadRepository.GetExternalDonorCodesLastUpdatedBefore(registryCode,
                DatabaseDonorType.Cord, lastUpdatedBeforeDate);
            var allDonors = donors.Concat(cords);

            return new JsonResult(allDonors);
        }

        [FunctionName(nameof(DeleteDonors))]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task DeleteDonors(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = RoutePrefix + "delete")]
            [RequestBodyType(typeof(string[]), "External donor codes")]
            HttpRequest request)
        {
            var donorCodes = await request.DeserialiseRequestBody<string[]>();
            var atlasIds = await donorReadRepository.GetDonorIdsByExternalDonorCodes(donorCodes);
            await donorImportRepository.DeleteDonorBatch(atlasIds.Values.ToList());
        }

        [FunctionName(nameof(GetRandomDonors))]
        [ProducesResponseType(typeof(IEnumerable<Donor>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRandomDonors(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RoutePrefix}random")]
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