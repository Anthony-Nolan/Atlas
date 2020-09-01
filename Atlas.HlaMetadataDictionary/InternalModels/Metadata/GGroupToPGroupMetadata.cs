using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface IGGroupToPGroupMetadata : ISerialisableHlaMetadata
    {
        SerialisedPGroup Serialised { get; }
    }

    public class SerialisedPGroup
    {
        public string PGroup { get; set; }
    }

    internal class GGroupToPGroupMetadata : SerialisableHlaMetadata, IGGroupToPGroupMetadata
    {
        public SerialisedPGroup Serialised { get; }
        public override object HlaInfoToSerialise => Serialised;

        public GGroupToPGroupMetadata(Locus locus, string lookupName, string pGroup)
            : base(locus, lookupName, TypingMethod.Molecular)
        {
            Serialised = new SerialisedPGroup {PGroup = pGroup};
        }
    }
}
