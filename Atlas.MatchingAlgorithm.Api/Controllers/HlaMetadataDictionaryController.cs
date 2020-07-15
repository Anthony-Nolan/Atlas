using System;
using System.Collections.Generic;
using System.Reflection;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    [Route("hla-metadata-dictionary")]
    public class HlaMetadataDictionaryController : ControllerBase
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public HlaMetadataDictionaryController(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            // TODO: ATLAS-355: Remove the need for a hardcoded default value
            var hlaVersionOrDefault = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist()
                ? hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion()
                : HlaMetadataDictionaryConstants.NoActiveVersionValue;

            hlaMetadataDictionary = factory.BuildDictionary(hlaVersionOrDefault);
        }

        [HttpPost]
        [Route("create-latest-version")]
        public async Task CreateLatestHlaMetadataDictionary()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
        }

        [HttpPost]
        [Route("create-specific-version")]
        public async Task CreateSpecificHlaMetadataDictionary(string version)
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(version));
        }

        [HttpPost]
        [Route("recreate-active-version")]
        public async Task RecreateActiveHlaMetadataDictionary()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Active);
        }

        /// <summary>
        /// Gets all pre-calculated HLA metadata to the specified version.
        /// Note: none of the returned data is persisted.
        /// Used when manually refreshing the contents of the file-backed HMD.
        /// </summary>
        [HttpPost]
        [Route("regenerate-metadata-file")]
        public string RegenerateNewMetadataFile(string version, string outputPath = null)
        {
            var metadata = hlaMetadataDictionary.GenerateAllHlaMetadata(version);
            var stringRepresentation = SerialiseToJsonString(metadata);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                System.IO.File.WriteAllText(outputPath, stringRepresentation);
            }
            return stringRepresentation;
        }

        private static string SerialiseToJsonString(HlaMetadataCollection metadata)
        {
            var jsonSerializerSettings = new JsonSerializerSettings {ContractResolver = new IgnoreHlaInfoToSerialise()};
            return JsonConvert.SerializeObject(metadata, Formatting.Indented, jsonSerializerSettings);
        }

        public class IgnoreHlaInfoToSerialise : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                //It seems to check both the interface AND the concrete class and writes if EITHER are non-Ignored, and we can't [JsonIgnore] the prop on the interface.
                if (property.PropertyName == nameof(ISerialisableHlaMetadata.HlaInfoToSerialise))
                {
                    property.ShouldSerialize = _ => false;
                    property.Ignored = true;
                }

                return property;
            }
        }
    }
}