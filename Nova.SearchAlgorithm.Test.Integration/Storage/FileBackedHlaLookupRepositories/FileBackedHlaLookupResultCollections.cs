using Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
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
    }
}
