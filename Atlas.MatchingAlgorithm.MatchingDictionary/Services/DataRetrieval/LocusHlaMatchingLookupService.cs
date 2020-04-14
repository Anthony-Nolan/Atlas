using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Handles matching HLA lookup logic at the locus-level,
    /// including handling of null-expressing alleles within the typing.
    /// </summary>
    public interface ILocusHlaMatchingLookupService
    {
        Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetHlaMatchingLookupResults(
            Locus locus,
            Tuple<string, string> locusTyping,
            string hlaDatabaseVersion);
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
            Locus locus,
            Tuple<string, string> locusTyping,
            string hlaDatabaseVersion)
        {
            var locusLookupResults = await GetLocusLookupResults(locus, locusTyping, hlaDatabaseVersion);

            var result1 = HandleNullAlleles(locusLookupResults[0], locusLookupResults[1]);
            var result2 = HandleNullAlleles(locusLookupResults[1], locusLookupResults[0]);

            return new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(result1, result2);
        }

        private async Task<IHlaMatchingLookupResult[]> GetLocusLookupResults(
            Locus locus,
            Tuple<string, string> locusHlaTyping,
            string hlaDatabaseVersion)
        {
            return await Task.WhenAll(
                singleHlaLookupService.GetHlaLookupResult(locus, locusHlaTyping.Item1, hlaDatabaseVersion),
                singleHlaLookupService.GetHlaLookupResult(locus, locusHlaTyping.Item2, hlaDatabaseVersion));
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
                lookupResult.Locus,
                lookupResult.LookupName,
                lookupResult.TypingMethod,
                lookupResult.MatchingPGroups.Union(otherLookupResult.MatchingPGroups));
        }
    }
}