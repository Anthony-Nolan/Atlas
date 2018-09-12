using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Handles matching HLA lookup logic at the locus-level,
    /// including handling of null-expressing alleles within the typing.
    /// </summary>
    public interface ILocusHlaMatchingLookupService
    {
        Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetHlaMatchingLookupResults(
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

        public async Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetHlaMatchingLookupResults(
            MatchLocus matchLocus,
            Tuple<string, string> locusTyping)
        {
            var locusLookupResults = await GetLocusLookupResults(matchLocus, locusTyping);

            var result1 = HandleNullAlleles(locusLookupResults[0], locusLookupResults[1]);
            var result2 = HandleNullAlleles(locusLookupResults[1], locusLookupResults[0]);

            return new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(result1, result2);
        }

        private async Task<IHlaMatchingLookupResult[]> GetLocusLookupResults(
            MatchLocus matchLocus, 
            Tuple<string, string> locusHlaTyping)
        {
            return await Task.WhenAll(
                singleHlaLookupService.GetHlaLookupResult(matchLocus, locusHlaTyping.Item1),
                singleHlaLookupService.GetHlaLookupResult(matchLocus, locusHlaTyping.Item2));
        }

        private static IHlaMatchingLookupResult HandleNullAlleles(
            IHlaMatchingLookupResult lookupResult,
            IHlaMatchingLookupResult otherLookupResult)
        {
            return lookupResult.IsNullExpressingTyping
                ? MergeMatchingHla(lookupResult, otherLookupResult)
                : lookupResult;
        }

        private static IHlaMatchingLookupResult MergeMatchingHla(
            IHlaMatchingLookupResult lookupResult,
            IHlaMatchingLookupResult otherLookupResult)
        {
            return new HlaMatchingLookupResult(
                lookupResult.MatchLocus,
                lookupResult.LookupName,
                lookupResult.TypingMethod,
                lookupResult.MatchingPGroups.Union(otherLookupResult.MatchingPGroups));
        }
    }
}