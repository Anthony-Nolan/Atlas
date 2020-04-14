using Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    /// <summary>
    /// Holds collections of HLA lookup results that have been
    /// imported from file.
    /// </summary>
    public class FileBackedHlaLookupResultCollections
    {
        public IEnumerable<FileBackedAlleleNameLookupResult> AlleleNameLookupResults { get; set; }
        public IEnumerable<FileBackedHlaMatchingLookupResult> HlaMatchingLookupResults { get; set; }
        public IEnumerable<FileBackedHlaScoringLookupResult> HlaScoringLookupResults { get; set; }
        public IEnumerable<FileBackedDpb1TceGroupsLookupResult> Dpb1TceGroupLookupResults { get; set; }
    }
}
