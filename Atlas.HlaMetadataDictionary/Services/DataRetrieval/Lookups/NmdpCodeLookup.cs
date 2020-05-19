using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.MultipleAlleleCodeDictionary;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class NmdpCodeLookup : AlleleNamesLookupBase
    {
        private readonly INmdpCodeCache nmdpCodeCache;

        public NmdpCodeLookup(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            INmdpCodeCache nmdpCodeCache)
            : base(hlaLookupRepository, alleleNamesLookupService)
        {
            this.nmdpCodeCache = nmdpCodeCache;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName)
        {
            return await nmdpCodeCache.GetOrAddAllelesForNmdpCode(locus, lookupName);
        }
    }
}