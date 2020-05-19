using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.MultipleAlleleCodeDictionary;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    ///  Consolidates HLA info used in matching for all alleles that map to the hla name.
    /// </summary>
    public interface IHlaMatchingLookupService : IHlaSearchingLookupService<IHlaMatchingLookupResult>
    {
        IEnumerable<string> GetAllPGroups(string hlaDatabaseVersion);
    }

    public class HlaMatchingLookupService : 
        HlaSearchingLookupServiceBase<IHlaMatchingLookupResult>, 
        IHlaMatchingLookupService
    {
        private readonly IHlaMatchingLookupRepository typedMatchingRepository;

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
            typedMatchingRepository = hlaMatchingLookupRepository;
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

        public IEnumerable<string> GetAllPGroups(string hlaDatabaseVersion)
        {
            return typedMatchingRepository.GetAllPGroups(hlaDatabaseVersion);
        }

    }
}