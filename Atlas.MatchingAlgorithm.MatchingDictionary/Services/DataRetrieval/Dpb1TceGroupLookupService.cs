using Atlas.Utils.Hla.Services;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Caching;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    ///  Consolidates TCE group assignments for DPB1 alleles.
    /// </summary>
    public interface IDpb1TceGroupLookupService : IHlaSearchingLookupService<IDpb1TceGroupsLookupResult>
    {
        Task<string> GetDpb1TceGroup(string dpb1HlaName, string hlaDatabaseVersion);
    }

    public class Dpb1TceGroupLookupService : 
        HlaSearchingLookupServiceBase<IDpb1TceGroupsLookupResult>, 
        IDpb1TceGroupLookupService
    {
        private const string NoTceGroupAssignment = "";

        public Dpb1TceGroupLookupService(
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            INmdpCodeCache cache
        ) : base(
            dpb1TceGroupsLookupRepository,
            alleleNamesLookupService,
            hlaCategorisationService,
            alleleSplitter,
            cache
            )
        {
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName, string hlaDatabaseVersion)
        {
            var lookupResult = await GetHlaLookupResult(Locus.Dpb1, dpb1HlaName, hlaDatabaseVersion);
            return lookupResult.TceGroup;
        }

        protected override IEnumerable<IDpb1TceGroupsLookupResult> ConvertTableEntitiesToLookupResults(
            IEnumerable<HlaLookupTableEntity> lookupTableEntities)
        {
            return lookupTableEntities.Select(entity => entity.ToDpb1TceGroupLookupResult());
        }

        protected override IDpb1TceGroupsLookupResult ConsolidateHlaLookupResults(
            Locus locus, 
            string lookupName,
            IEnumerable<IDpb1TceGroupsLookupResult> lookupResults)
        {
            var results = lookupResults.ToList();

            var tceGroups = results
                .Select(lookupResult => lookupResult.TceGroup)
                .Distinct()
                .ToList();

            // If a DPB1 typing maps >1 TCE group, then it should be treated the same as an allele
            // that has no TCE group assignment.
            var tceGroup = tceGroups.Count == 1 ? tceGroups.Single() : NoTceGroupAssignment;

            return new Dpb1TceGroupsLookupResult(lookupName, tceGroup);
        }
    }
}