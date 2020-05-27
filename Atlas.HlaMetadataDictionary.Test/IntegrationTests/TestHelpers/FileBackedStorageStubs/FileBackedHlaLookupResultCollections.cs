using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
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
