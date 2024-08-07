//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Threading.Tasks;
//using Atlas.Common.Public.Models.GeneticData;
//using Atlas.Common.Utils;
//using Atlas.Common.Utils.Http;
//using Atlas.HlaMetadataDictionary.ExternalInterface;
//using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
//using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
//using Atlas.MatchingAlgorithm.Functions.Models.Debug;
//using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
//using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
//using AzureFunctions.Extensions.Swashbuckle.Attribute;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Functions.Worker;
//using Newtonsoft.Json;

//namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
//{
//    public class HlaMetadataDictionaryFunctions //TODO: ATLAS-262 - migrate to new project
//    {
//        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

//        public HlaMetadataDictionaryFunctions(
//            IHlaMetadataDictionaryFactory factory,
//            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
//        {
//            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
//        }

//        [Function(nameof(ConvertHla))]
//        [ProducesResponseType(typeof(IEnumerable<string>), (int)HttpStatusCode.OK)]
//        public async Task<IActionResult> ConvertHla(
//            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(ConvertHla)}")]
//            [RequestBodyType(typeof(HlaConversionRequest), nameof(HlaConversionRequest))]
//            HttpRequest httpRequest)
//        {
//            try
//            {
//                var version = JsonConvert.DeserializeObject<HlaConversionRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
//                return new JsonResult(await hlaMetadataDictionary.ConvertHla(version.Locus, version.HlaName, version.TargetHlaCategory));

//            }
//            catch (Exception ex)
//            {
//                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to convert HLA.", ex);
//            }
//        }

//        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
//        [Function(nameof(ScoringMetadata))]
//        [ProducesResponseType(typeof(IHlaScoringMetadata), (int)HttpStatusCode.OK)]
//        public async Task<IActionResult> ScoringMetadata(
//            [HttpTrigger(AuthorizationLevel.Function, "get", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(ScoringMetadata)}/"+"{locusName}/{hlaName}")]
//            HttpRequest httpRequest,
//            string locusName,
//            string hlaName)
//        {
//            try
//            {
//                var locus = Enum.Parse<Locus>(locusName);
//                return new JsonResult(await hlaMetadataDictionary.GetHlaScoringMetadata(locus, hlaName));
//            }
//            catch (Exception ex)
//            {
//                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to retrieve scoring metadata.", ex);
//            }
//        }

//        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
//        [Function(nameof(Dpb1TceGroups))]
//        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
//        public async Task<IActionResult> Dpb1TceGroups(
//            [HttpTrigger(AuthorizationLevel.Function, "get", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(Dpb1TceGroups)}/"+"{hlaName}")]
//            HttpRequest httpRequest,
//            string hlaName)
//        {
//            try
//            {
//                return new JsonResult(await hlaMetadataDictionary.GetDpb1TceGroup(hlaName));

//            }
//            catch (Exception ex)
//            {
//                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to retrieve TCE groups.", ex);
//            }
//        }

//        [Function(nameof(SerologyToAlleleMapping))]
//        [ProducesResponseType(typeof(IEnumerable<SerologyToAlleleMappingSummary>), (int)HttpStatusCode.OK)]
//        public async Task<IActionResult> SerologyToAlleleMapping(
//            [HttpTrigger(
//                AuthorizationLevel.Function,
//                "get",
//                Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(SerologyToAlleleMapping)}/"+"{locusName}/{serologyName}/{pGroup?}")]
//            HttpRequest httpRequest,
//            string locusName,
//            string serologyName,
//            string pGroup)
//        {
//            try
//            {
//                var formattedLocusName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(locusName.ToLower());
//                if (Enum.TryParse(formattedLocusName, out Locus locus))
//                {
//                    var mappings = await hlaMetadataDictionary.GetSerologyToAlleleMappings(locus, serologyName);

//                    var result = string.IsNullOrEmpty(pGroup)
//                        ? mappings.OrderBy(m => m.PGroup).ThenBy(m => m.SerologyBridge)
//                        : mappings.Where(m => m.PGroup == pGroup).OrderBy(m => m.SerologyBridge);

//                    return new JsonResult(result);
//                }

//                throw new ArgumentException($"{locusName} is not a valid option for type, {nameof(Locus)}.");

//            }
//            catch (Exception ex)
//            {
//                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Failed to retrieve serology to allele mappings.", ex);
//            }
//        }
//    }
//}