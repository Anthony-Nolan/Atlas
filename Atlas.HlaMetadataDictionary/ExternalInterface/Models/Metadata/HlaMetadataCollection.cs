using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    public class HlaMetadataCollection
    {
        public List<IHlaMetadata> AlleleNameMetadata { get; set; }
        public List<IHlaMetadata> HlaMatchingMetadata { get; set; }
        public List<IHlaMetadata> HlaScoringMetadata { get; set; }
        public List<IHlaMetadata> Dpb1TceGroupMetadata { get; set; }
    }

    internal class HlaMetadataCollectionForSerialisation
    {
        public IEnumerable<ISerialisableHlaMetadata> AlleleNameMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaMatchingMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaScoringMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> Dpb1TceGroupMetadata { get; set; }

        public HlaMetadataCollection ToExternalCollection()
        {
            return new HlaMetadataCollection
            {
                AlleleNameMetadata = AlleleNameMetadata.Cast<IHlaMetadata>().ToList(),
                HlaMatchingMetadata = HlaMatchingMetadata.Cast<IHlaMetadata>().ToList(),
                HlaScoringMetadata = HlaScoringMetadata.Cast<IHlaMetadata>().ToList(),
                Dpb1TceGroupMetadata = Dpb1TceGroupMetadata.Cast<IHlaMetadata>().ToList(),
            };
        }
    }
}
