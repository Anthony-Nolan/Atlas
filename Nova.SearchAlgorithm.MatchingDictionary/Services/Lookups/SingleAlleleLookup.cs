using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class SingleAlleleLookup : AlleleNamesLookupBase
    {
        public SingleAlleleLookup(
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService)
            : base(hlaMatchingLookupRepository, alleleNamesLookupService)
        {
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(MatchLocus matchLocus, string lookupName)
        {
            return await Task.FromResult(
                (IEnumerable<string>)new[] { lookupName });
        }
    }
}