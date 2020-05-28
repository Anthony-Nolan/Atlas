using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.Lookups
{
    public class HlaLookupResultCollections
    {
        public IEnumerable<ISerialisableHlaMetadata> AlleleNameLookupResults { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaMatchingLookupResults { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaScoringLookupResults { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> Dpb1TceGroupLookupResults { get; set; }
    }
}
