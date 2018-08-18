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
    public interface IDpb1TceGroupsLookupService : IHlaSearchingLookupService<IDpb1TceGroupsLookupResult>
    {
        Task<IDpb1TceGroupsLookupResult> GetDpb1TceGroupsLookupResult(string hlaName);
    }

    public class Dpb1TceGroupsLookupService : 
        HlaSearchingLookupServiceBase<IDpb1TceGroupsLookupResult>, 
        IDpb1TceGroupsLookupService
    {
        public Dpb1TceGroupsLookupService(
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

        public async Task<IDpb1TceGroupsLookupResult> GetDpb1TceGroupsLookupResult(string hlaName)
        {
            return await GetHlaLookupResult(MatchLocus.Dpb1, hlaName);
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
                .SelectMany(lookupResult => lookupResult.TceGroups)
                .Distinct();

            return new Dpb1TceGroupsLookupResult(
                lookupName,
                tceGroups);
        }
    }
}