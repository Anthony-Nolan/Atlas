using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    /// <summary>
    /// Holds set of HLA Metadata that has been imported from file.
    /// </summary>
    public class FileBackedHlaMetadataCollection
    {
        public IEnumerable<FileBackedAlleleNameMetadata> AlleleNameMetadata { get; set; }
        public IEnumerable<FileBackedHlaMatchingMetadata> HlaMatchingMetadata { get; set; }
        public IEnumerable<FileBackedHlaScoringMetadata> HlaScoringMetadata { get; set; }
        public IEnumerable<FileBackedDpb1TceGroupsMetadata> Dpb1TceGroupMetadata { get; set; }
        public IEnumerable<FileBackedAlleleGroupMetadata> AlleleGroupMetadata { get; set; }
        public IEnumerable<FileBackedMolecularTypingToPGroupMetadata> GGroupToPGroupMetadata { get; set; }
        public IEnumerable<FileBackedSmallGGroupsMetadata> SmallGGroupMetadata { get; set; }
        public IEnumerable<FileBackedMolecularTypingToPGroupMetadata> SmallGGroupToPGroupMetadata { get; set; }
        public IEnumerable<FileBackedSerologyToAllelesMetadata> SerologyToAllelesMetadata { get; set; }
    }
}
