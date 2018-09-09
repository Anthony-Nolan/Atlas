using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using System;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Handles matching HLA lookup logic at the locus-level,
    /// e.g., null allele handling.
    /// </summary>
    public interface ILocusHlaMatchingLookupService
    {
        Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetHlaMatchingLookupResultForLocus(
            MatchLocus matchLocus, Tuple<string, string> locusTyping);
    }

    /// <inheritdoc />
    public class LocusHlaMatchingLookupService : ILocusHlaMatchingLookupService
    {
        private readonly IHlaMatchingLookupService singleHlaLookupService;

        public LocusHlaMatchingLookupService(IHlaMatchingLookupService singleHlaLookupService)
        {
            this.singleHlaLookupService = singleHlaLookupService;
        }

        public async Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetHlaMatchingLookupResultForLocus(
            MatchLocus matchLocus, 
            Tuple<string, string> locusTyping)
        {
            var lookupResult = await Task.WhenAll(
                singleHlaLookupService.GetHlaLookupResult(matchLocus, locusTyping.Item1),
                singleHlaLookupService.GetHlaLookupResult(matchLocus, locusTyping.Item2));

            return new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(lookupResult[0], lookupResult[1]);
        }
    }
}
