using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    [Route("hla-metadata-dictionary")] //TODO: ATLAS-262 (MDM) migrate to new project
    public class HlaMetadataDictionaryController : ControllerBase
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public HlaMetadataDictionaryController(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
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
    }
}