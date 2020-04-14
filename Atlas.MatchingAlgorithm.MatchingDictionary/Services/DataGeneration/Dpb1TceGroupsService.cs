using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Generates a complete collection of DPB1 TCE Group lookup results.
    /// </summary>
    public interface IDpb1TceGroupsService
    {
        IEnumerable<IDpb1TceGroupsLookupResult> GetDpb1TceGroupLookupResults(string hlaDatabaseVersion);
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

        public IEnumerable<IDpb1TceGroupsLookupResult> GetDpb1TceGroupLookupResults(string hlaDatabaseVersion)
        {
            var allResults = wmdaDataRepository
                .GetWmdaDataset(hlaDatabaseVersion)
                .Dpb1TceGroupAssignments
                .SelectMany(GetLookupResultPerDpb1LookupName);

            return GroupResultsByLookupName(allResults);
        }

        private static IEnumerable<IDpb1TceGroupsLookupResult> GetLookupResultPerDpb1LookupName(Dpb1TceGroupAssignment tceGroupAssignment)
        {
            var lookupNames = GetLookupNames(tceGroupAssignment);

            return lookupNames.Select(name => new Dpb1TceGroupsLookupResult(name, tceGroupAssignment.VersionTwoAssignment));
        }

        private static IEnumerable<string> GetLookupNames(IWmdaHlaTyping tceGroup)
        {
            var allele = new AlleleTyping(Locus.Dpb1, tceGroup.Name);
            return new[]
            {
                allele.Name,
                allele.ToXxCodeLookupName()
            }
            .Concat(allele.ToNmdpCodeAlleleLookupNames());
        }

        /// <summary>
        /// Due to DPB1 nomenclature, DPB1* expressing alleles with the same lookup name
        /// e.g., [0-9]+:XX, will all have the same protein, and thus the same TCE group.
        /// If a group of alleles with the same lookup name contains a null allele, the assignment
        /// of the expressing alleles should be preferred.
        /// </summary>
        private static IEnumerable<IDpb1TceGroupsLookupResult> GroupResultsByLookupName(
            IEnumerable<IDpb1TceGroupsLookupResult> results)
        {
            return results
                .GroupBy(result => result.LookupName)
                .Select(grp => new Dpb1TceGroupsLookupResult(
                    grp.Key,
                    grp.Select(lookup => lookup.TceGroup)
                        .Distinct()
                        .OrderByDescending(tceGroup => tceGroup)
                        .First()));
        }
    }
}
