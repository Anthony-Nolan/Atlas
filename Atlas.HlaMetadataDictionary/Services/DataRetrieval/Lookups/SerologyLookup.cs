using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Utils.Models;

namespace Atlas.HlaMetadataDictionary.Services.Lookups
{
    internal class SerologyLookup : HlaLookupBase
    {
        public SerologyLookup(IHlaLookupRepository hlaLookupRepository) : base(hlaLookupRepository)
        {
        }

        public override async Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var entity = await GetHlaLookupTableEntityIfExists(locus, lookupName, TypingMethod.Serology, hlaDatabaseVersion);
            return new List<HlaLookupTableEntity> { entity };
        }
    }
}
