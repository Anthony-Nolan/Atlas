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
            var stringRep = SerialiseToJsonString(metadata);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                System.IO.File.WriteAllText(outputPath, stringRep);
            }
            return stringRep;
        }

        private static string SerialiseToJsonString(HlaMetadataCollection metadata)
        {
            var contractResolver = new TargetedIgnoringContractResolver();
            contractResolver.IgnorePropertyOnAllTypes(nameof(ISerialisableHlaMetadata.HlaInfoToSerialise)); //It seems to check both the interface AND the concrete class and writes if EITHER are non-Ignored.
            var jsonSerializerSettings = new JsonSerializerSettings {ContractResolver = contractResolver};

            var stringRep = JsonConvert.SerializeObject(metadata, Formatting.Indented, jsonSerializerSettings);
            return stringRep;
        }

        public class TargetedIgnoringContractResolver : DefaultContractResolver
        {
            private readonly Dictionary<Type, HashSet<string>> typedPropsToIgnore = new Dictionary<Type, HashSet<string>>();
            private readonly HashSet<string> globalPropsToIgnore = new HashSet<string>();

            public void IgnorePropertyOnAllTypes(string jsonPropertyName)
            {
                globalPropsToIgnore.Add(jsonPropertyName);
            }

            public void IgnoreProperty(Type type, string jsonPropertyName)
            {
                typedPropsToIgnore.TryAdd(type, new HashSet<string>());

                typedPropsToIgnore[type].Add(jsonPropertyName);
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if (ShouldBeIgnored(property.DeclaringType, property.PropertyName))
                {
                    property.ShouldSerialize = _ => false;
                    property.Ignored = true;
                }

                return property;
            }

            private bool ShouldBeIgnored(Type type, string jsonPropertyName)
            {
                return globalPropsToIgnore.Contains(jsonPropertyName) ||
                       (typedPropsToIgnore.ContainsKey(type) && typedPropsToIgnore[type].Contains(jsonPropertyName));
            }
        }

    }
}