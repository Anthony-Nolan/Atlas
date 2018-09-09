using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using System;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface ILocusHlaMatchingLookupService
    {
        Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetHlaMatchingLookupResultForLocus(
            MatchLocus matchLocus, Tuple<string, string> locusTyping);
    }

    public class LocusHlaMatchingLookupService : ILocusHlaMatchingLookupService
    {
        private readonly IHlaMatchingLookupService matchingLookupService;

        public LocusHlaMatchingLookupService(IHlaMatchingLookupService matchingLookupService)
        {
            this.matchingLookupService = matchingLookupService;
        }

        public async Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetHlaMatchingLookupResultForLocus(
            MatchLocus matchLocus, 
            Tuple<string, string> locusTyping)
        {
            var lookupResult = await Task.WhenAll(
                matchingLookupService.GetHlaLookupResult(matchLocus, locusTyping.Item1),
                matchingLookupService.GetHlaLookupResult(matchLocus, locusTyping.Item2));

            return new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(lookupResult[0], lookupResult[1]);
        }
    }
}
