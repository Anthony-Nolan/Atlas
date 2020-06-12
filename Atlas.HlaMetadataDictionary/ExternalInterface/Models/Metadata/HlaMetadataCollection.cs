﻿using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    internal class HlaMetadataCollectionForSerialisation
    {
        public IEnumerable<ISerialisableHlaMetadata> AlleleNameMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaMatchingMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> HlaScoringMetadata { get; set; }
        public IEnumerable<ISerialisableHlaMetadata> Dpb1TceGroupMetadata { get; set; }
    }
}
