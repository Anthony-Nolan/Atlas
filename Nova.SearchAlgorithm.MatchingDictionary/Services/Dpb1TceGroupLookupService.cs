using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    ///  Consolidates TCE group assignments for DPB1 alleles.
    /// </summary>
    public interface IDpb1TceGroupLookupService : IHlaSearchingLookupService<IDpb1TceGroupsLookupResult>
    {
        Task<string> GetDpb1TceGroup(string dpb1HlaName);
    }

    public class Dpb1TceGroupLookupService : 
        HlaSearchingLookupServiceBase<IDpb1TceGroupsLookupResult>, 
        IDpb1TceGroupLookupService
    {
        private const string NoTceGroupAssignment = "";

        public Dpb1TceGroupLookupService(
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger
        ) : base(
            dpb1TceGroupsLookupRepository,
            alleleNamesLookupService,
            hlaServiceClient,
            hlaCategorisationService,
            alleleSplitter,
            memoryCache,
            logger
            )
        {
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            var lookupResult = await GetHlaLookupResult(MatchLocus.Dpb1, dpb1HlaName);
            return lookupResult.TceGroup;
        }

        protected override IEnumerable<IDpb1TceGroupsLookupResult> ConvertTableEntitiesToLookupResults(
            IEnumerable<HlaLookupTableEntity> lookupTableEntities)
        {
            return lookupTableEntities.Select(entity => entity.ToDpb1TceGroupLookupResult());
        }

        protected override IDpb1TceGroupsLookupResult ConsolidateHlaLookupResults(
            MatchLocus matchLocus, 
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