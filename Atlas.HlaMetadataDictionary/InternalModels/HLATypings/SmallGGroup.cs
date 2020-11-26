using System.Collections.Generic;
using Atlas.Common.GeneticData;

namespace Atlas.HlaMetadataDictionary.InternalModels.HLATypings
{
    internal class SmallGGroup
    {
        public Locus Locus { get; set; }
        public string Name { get; set; }
        public IReadOnlyCollection<string> Alleles { get; set; }

        // TODO: ATLAS-880 - Add P group mapping
        public string PGroup { get; set; }
    }
}
