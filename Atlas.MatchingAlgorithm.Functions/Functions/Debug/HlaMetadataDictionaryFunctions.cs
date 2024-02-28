using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Http;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Functions.Models.Debug;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
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

namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
{
    public class HlaMetadataDictionaryFunctions //TODO: ATLAS-262 - migrate to new project
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly LogsQueryClient logsQueryClient;
        private readonly AzureMonitoringSettings azureMonitoringSettings;

        private const string HlaExpansionFailuresQuery = @"
            AppEvents
            | where Name startswith ""HLA Expansion""
            | extend
                DonorInfo = parse_json(tostring(Properties[""DonorInfo""])),
                Locus = tostring(Properties[""Locus""]),
                HlaName = tostring(Properties[""HlaName""])
            | distinct
                InvalidHLA = strcat(Locus, HlaName),
                ExternalDonorCode = tostring(DonorInfo[""ExternalDonorCode""]),
                ExceptionType = tostring(Properties[""InnerExceptionType""])
            | summarize
                ExceptionType = make_list(ExceptionType, 10)[0],
                ExternalDonorCodes = make_list(ExternalDonorCode, 1000),
                DonorCount = count() by InvalidHLA
            | order by DonorCount desc";

        public HlaMetadataDictionaryFunctions(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            LogsQueryClient logsQueryClient,
            IOptions<AzureMonitoringSettings> azureMonitoringSettings)
        {
            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
            this.logsQueryClient = logsQueryClient;
            this.azureMonitoringSettings = azureMonitoringSettings.Value;
        }

        [FunctionName(nameof(ConvertHla))]
        public async Task<IEnumerable<string>> ConvertHla(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(ConvertHla)}")]
            [RequestBodyType(typeof(HlaConversionRequest), nameof(HlaConversionRequest))]
            HttpRequest httpRequest)
        {
            try
            {
                var version = JsonConvert.DeserializeObject<HlaConversionRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
                return await hlaMetadataDictionary.ConvertHla(version.Locus, version.HlaName, version.TargetHlaCategory);

            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to convert HLA.", ex);
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(ScoringMetadata))]
        public async Task<IHlaScoringMetadata> ScoringMetadata(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(ScoringMetadata)}/"+"{locusName}/{hlaName}")]
            HttpRequest httpRequest,
            string locusName,
            string hlaName)
        {
            try
            {
                var locus = Enum.Parse<Locus>(locusName);
                return await hlaMetadataDictionary.GetHlaScoringMetadata(locus, hlaName);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to retrieve scoring metadata.", ex);
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(Dpb1TceGroups))]
        public async Task<string> Dpb1TceGroups(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(Dpb1TceGroups)}/"+"{hlaName}")]
            HttpRequest httpRequest,
            string hlaName)
        {
            try
            {
                return await hlaMetadataDictionary.GetDpb1TceGroup(hlaName);

            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to retrieve TCE groups.", ex);
            }
        }

        [FunctionName(nameof(SerologyToAlleleMapping))]
        public async Task<IEnumerable<SerologyToAlleleMappingSummary>> SerologyToAlleleMapping(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(SerologyToAlleleMapping)}/"+"{locusName}/{serologyName}/{pGroup?}")]
            HttpRequest httpRequest,
            string locusName,
            string serologyName,
            string pGroup)
        {
            try
            {
                var formattedLocusName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(locusName.ToLower());
                if (Enum.TryParse(formattedLocusName, out Locus locus))
                {
                    var mappings = await hlaMetadataDictionary.GetSerologyToAlleleMappings(locus, serologyName);

                    return string.IsNullOrEmpty(pGroup)
                        ? mappings.OrderBy(m => m.PGroup).ThenBy(m => m.SerologyBridge)
                        : mappings.Where(m => m.PGroup == pGroup).OrderBy(m => m.SerologyBridge);
                }

                throw new ArgumentException($"{locusName} is not a valid option for type, {nameof(Locus)}.");

            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to retrieve serology to allele mappings.", ex);
            }
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
            var response = await logsQueryClient.QueryWorkspaceAsync(azureMonitoringSettings.WorkspaceId, HlaExpansionFailuresQuery, new QueryTimeRange(TimeSpan.FromDays(daysToQuery ?? 14)));
            var result = response.Value;
            var output = new JArray();

            foreach (var row in result.Table.Rows) 
            {
                var outputRow = new JObject();

                foreach (var (name, value) in result.Table.Columns.Select(col => (name: col.Name, value: row[col.Name])))
                {
                    outputRow.Add(
                        name, 
                        value is BinaryData binaryData ? JToken.FromObject(binaryData.ToString()) : JToken.FromObject(value));
                }


                output.Add(outputRow);
            }
            
            return new ContentResult 
            { 
                Content = output.ToString(), 
                StatusCode = StatusCodes.Status200OK, 
                ContentType = ContentType.ApplicationJson.ToString() 
            };
        }
    }
}