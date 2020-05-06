using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class SingleAlleleLookup : AlleleNamesLookupBase
    {
        public SingleAlleleLookup(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService)
            : base(hlaLookupRepository, alleleNamesLookupService)
        {
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName)
        {
            return await Task.FromResult((IEnumerable<string>)new[] { lookupName });
        }
    }
}