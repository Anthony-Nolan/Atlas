using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
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