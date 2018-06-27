using Nova.HLAService.Client;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class AlleleStringLookup : MultipleAllelesLookup
    {
        public AlleleStringLookup(IMatchingDictionaryRepository dictionaryRepository, IHlaServiceClient hlaServiceClient) 
        : base(dictionaryRepository, hlaServiceClient)
        {
        }

        protected override async Task<IEnumerable<string>> GetAlleles(MatchLocus matchLocus, string lookupName)
        {
            // TODO: Use library version of string splitter, not via HTTP
            return await HlaServiceClient.GetAlleleNamesFromAlleleString(lookupName);
        }
    }
}
