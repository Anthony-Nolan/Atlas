using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
        [HttpGet]
        [Route("all-metadata")]
        public HlaMetadataCollection GetAllActiveHlaMetadata(string version)
        {
            return hlaMetadataDictionary.GetAllHlaMetadata(version);
        }
    }
}