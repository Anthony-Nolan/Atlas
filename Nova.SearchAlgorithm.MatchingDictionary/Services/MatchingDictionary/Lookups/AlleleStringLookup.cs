using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class AlleleStringLookup : AlleleNameBasedLookup
    {
        private readonly IAlleleStringSplitterService alleleSplitter;
        
        public AlleleStringLookup(
            IMatchingDictionaryRepository dictionaryRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IAlleleStringSplitterService alleleSplitter)
            : base(dictionaryRepository, alleleNamesLookupService)
        {
            this.alleleSplitter = alleleSplitter;
        }

        protected override async Task<IEnumerable<string>> GetAllelesNames(MatchLocus matchLocus, string lookupName)
        {
            return await Task.Run(() => alleleSplitter.GetAlleleNamesFromAlleleString(lookupName));
        }
    }
}
