using System.Collections.Generic;
using Atlas.Common.GeneticData;

namespace Atlas.HlaMetadataDictionary.InternalModels.HLATypings
{
    internal class SmallGGroup
    {
        public Locus Locus { get; set; }
        public string Name { get; set; }
        public IReadOnlyCollection<string> Alleles { get; set; }
        public string PGroup { get; set; }
    }
}
