using System.Collections.Generic;
using System.Linq;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Caching;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
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
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IAntigenCache cache,
            ILogger logger
        ) : base(
            hlaMatchingLookupRepository,
            alleleNamesLookupService,
            hlaServiceClient,
            hlaCategorisationService,
            alleleSplitter,
            cache,
            logger
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