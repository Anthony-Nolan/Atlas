using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    /// Handles matching HLA lookup logic at the locus-level,
    /// including handling of null-expressing alleles within the typing.
    /// </summary>
    internal interface ILocusHlaMatchingLookupService
    {
        Task<LocusInfo<IHlaMatchingLookupResult>> GetHlaMatchingLookupResults(
            Locus locus,
            LocusInfo<string> locusTyping,
            string hlaNomenclatureVersion);
    }

    /// <inheritdoc />
    internal class LocusHlaMatchingLookupService : ILocusHlaMatchingLookupService
    {
        private readonly IHlaMatchingLookupService singleHlaLookupService;

        public LocusHlaMatchingLookupService(IHlaMatchingLookupService singleHlaLookupService)
        {
            this.singleHlaLookupService = singleHlaLookupService;
        }

        public async Task<LocusInfo<IHlaMatchingLookupResult>> GetHlaMatchingLookupResults(
            Locus locus,
            LocusInfo<string> locusTyping,
            string hlaNomenclatureVersion)
        {
            var locusLookupResults = await GetLocusLookupResults(locus, locusTyping, hlaNomenclatureVersion);

            var result1 = HandleNullAlleles(locusLookupResults[0], locusLookupResults[1]);
            var result2 = HandleNullAlleles(locusLookupResults[1], locusLookupResults[0]);

            return new LocusInfo<IHlaMatchingLookupResult>(result1, result2);
        }

        private async Task<IHlaMatchingLookupResult[]> GetLocusLookupResults(
            Locus locus,
            LocusInfo<string> locusHlaTyping,
            string hlaNomenclatureVersion)
        {
            return await Task.WhenAll(
                singleHlaLookupService.GetHlaLookupResult(locus, locusHlaTyping.Position1, hlaNomenclatureVersion),
                singleHlaLookupService.GetHlaLookupResult(locus, locusHlaTyping.Position2, hlaNomenclatureVersion));
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