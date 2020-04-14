using Nova.HLAService.Client.Services;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Caching;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    ///  Consolidates HLA info used in matching for all alleles that map to the hla name.
    /// </summary>
    public interface IHlaMatchingLookupService : IHlaSearchingLookupService<IHlaMatchingLookupResult>
    {
    }

    public class HlaMatchingLookupService : 
        HlaSearchingLookupServiceBase<IHlaMatchingLookupResult>, 
        IHlaMatchingLookupService
    {
        public HlaMatchingLookupService(
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            INmdpCodeCache cache
        ) : base(
            hlaMatchingLookupRepository,
            alleleNamesLookupService,
            hlaCategorisationService,
            alleleSplitter,
            cache
            )
        {
        }

        protected override IEnumerable<IHlaMatchingLookupResult> ConvertTableEntitiesToLookupResults(
            IEnumerable<HlaLookupTableEntity> lookupTableEntities)
        {
            return lookupTableEntities.Select(entity => entity.ToHlaMatchingLookupResult());
        }

        protected override IHlaMatchingLookupResult ConsolidateHlaLookupResults(
            Locus locus, 
            string lookupName,
            IEnumerable<IHlaMatchingLookupResult> lookupResults)
        {
            var results = lookupResults.ToList();

            var typingMethod = results
                .First()
                .TypingMethod;

            var pGroups = results
                .SelectMany(lookupResult => lookupResult.MatchingPGroups)
                .Distinct();

            return new HlaMatchingLookupResult(
                locus,
                lookupName,
                typingMethod,
                pGroups);
        }
    }
}