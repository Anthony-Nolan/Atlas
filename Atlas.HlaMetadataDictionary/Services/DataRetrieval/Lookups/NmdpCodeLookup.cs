using Atlas.MultipleAlleleCodeDictionary;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Utils.Models;

namespace Atlas.HlaMetadataDictionary.Services.Lookups
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