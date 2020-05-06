using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.Lookups
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