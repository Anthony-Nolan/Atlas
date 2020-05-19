using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
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
