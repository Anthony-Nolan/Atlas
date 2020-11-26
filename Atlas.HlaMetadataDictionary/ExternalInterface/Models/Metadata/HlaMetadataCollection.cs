using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    public class HlaMetadataCollection
    {
        public IEnumerable<ISerialisableHlaMetadata> AlleleNameMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaMatchingMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaScoringMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> Dpb1TceGroupMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> AlleleGroupMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> SmallGGroupMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> GGroupToPGroupMetadata { get; set; }
    }
}
