using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.Lookups
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