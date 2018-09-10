using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var locusLookupResult = await Task.WhenAll(
                singleHlaLookupService.GetHlaLookupResult(matchLocus, locusTyping.Item1),
                singleHlaLookupService.GetHlaLookupResult(matchLocus, locusTyping.Item2));

            var result1 = GetSingleHlaLookupResult(locusLookupResult[0], locusLookupResult[1].MatchingPGroups);
            var result2 = GetSingleHlaLookupResult(locusLookupResult[1], locusLookupResult[0].MatchingPGroups);

            return new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(result1, result2);
        }

        private static IHlaMatchingLookupResult GetSingleHlaLookupResult(
            IHlaMatchingLookupResult lookupResult,
            IEnumerable<string> additionalPGroups)
        {
            // TODO: NOVA-1723 - Replace Result.PGroups.Any() with Result.ContainsNullAllele
            return lookupResult.MatchingPGroups.Any()
                ? lookupResult
                : AddPGroupsToLookupResult(lookupResult, additionalPGroups);
        }

        private static IHlaMatchingLookupResult AddPGroupsToLookupResult(
            IHlaMatchingLookupResult result,
            IEnumerable<string> pGroupsToAdd)
        {
            return new HlaMatchingLookupResult(
                result.MatchLocus,
                result.LookupName,
                result.TypingMethod,
                result.MatchingPGroups.Union(pGroupsToAdd));
        }
    }
}