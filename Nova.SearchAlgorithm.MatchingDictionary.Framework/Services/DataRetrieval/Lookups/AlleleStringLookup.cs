using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class AlleleStringLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleStringSplitterService alleleSplitter;
        
        public AlleleStringLookup(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IAlleleStringSplitterService alleleSplitter)
            : base(hlaLookupRepository, alleleNamesLookupService)
        {
            this.alleleSplitter = alleleSplitter;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName)
        {
            return await Task.Run(() => alleleSplitter.GetAlleleNamesFromAlleleString(lookupName));
        }
    }
}
