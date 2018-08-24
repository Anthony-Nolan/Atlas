using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Generates a complete collection of DPB1 TCE Group lookup results.
    /// </summary>
    public interface IDpb1TceGroupsService
    {
        IEnumerable<IDpb1TceGroupsLookupResult> GetDpb1TceGroupLookupResults();
    }

    /// <inheritdoc />
    /// <summary>
    /// Extracts DPB1 TCE groups from a WMDA data repository.
    /// </summary>
    public class Dpb1TceGroupsService : IDpb1TceGroupsService
    {
        private readonly IWmdaDataRepository wmdaDataRepository;

        public Dpb1TceGroupsService(IWmdaDataRepository wmdaDataRepository)
        {
            this.wmdaDataRepository = wmdaDataRepository;
        }

        public IEnumerable<IDpb1TceGroupsLookupResult> GetDpb1TceGroupLookupResults()
        {
            var allResults = wmdaDataRepository
                .Dpb1TceGroupAssignments
                .SelectMany(GetLookupResultPerDpb1LookupName);

            return GroupResultsByLookupName(allResults);
        }

        private static IEnumerable<IDpb1TceGroupsLookupResult> GetLookupResultPerDpb1LookupName(Dpb1TceGroupAssignment tceGroupAssignment)
        {
            var lookupNames = GetLookupNames(tceGroupAssignment);
            var tceGroups = GetTceGroups(tceGroupAssignment);

            return lookupNames.Select(name => new Dpb1TceGroupsLookupResult(name, tceGroups));
        }

        private static IEnumerable<string> GetLookupNames(IWmdaHlaTyping tceGroup)
        {
            var allele = new AlleleTyping(MatchLocus.Dpb1, tceGroup.Name);
            return new[] {allele.Name, allele.ToNmdpCodeAlleleLookupName(), allele.ToXxCodeLookupName()};
        }

        private static IEnumerable<string> GetTceGroups(Dpb1TceGroupAssignment tceGroupAssignment)
        {
            var selectedVersion = tceGroupAssignment.VersionTwoAssignment;
            return string.IsNullOrEmpty(selectedVersion)
                ? new string[] { }
                : new[] {selectedVersion};
        }

        private static IEnumerable<IDpb1TceGroupsLookupResult> GroupResultsByLookupName(
            IEnumerable<IDpb1TceGroupsLookupResult> results)
        {
            return results
                .GroupBy(result => result.LookupName)
                .Select(grp => new Dpb1TceGroupsLookupResult(
                    grp.Key,
                    grp.SelectMany(lookup => lookup.TceGroups).Distinct()));
        }
    }
}
