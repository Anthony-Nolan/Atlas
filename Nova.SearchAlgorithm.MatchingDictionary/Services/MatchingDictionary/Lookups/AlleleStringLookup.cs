using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class AlleleStringLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleStringSplitterService alleleSplitter;
        
        public AlleleStringLookup(
            IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IAlleleStringSplitterService alleleSplitter)
            : base(preCalculatedHlaMatchRepository, alleleNamesLookupService)
        {
            this.alleleSplitter = alleleSplitter;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(MatchLocus matchLocus, string lookupName)
        {
            return await Task.Run(() => alleleSplitter.GetAlleleNamesFromAlleleString(lookupName));
        }
    }
}
