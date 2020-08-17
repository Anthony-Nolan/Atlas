using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface IGGroupToPGroupMetadata : ISerialisableHlaMetadata
    {
        public string PGroup { get; }
    }

    internal class GGroupToPGroupMetadata : SerialisableHlaMetadata, IGGroupToPGroupMetadata
    {
        public string PGroup { get; }
        public override object HlaInfoToSerialise => PGroup;

        public GGroupToPGroupMetadata(Locus locus, string lookupName, string pGroup)
            : base(locus, lookupName, TypingMethod.Molecular)
        {
            PGroup = pGroup;
        }
    }
}
