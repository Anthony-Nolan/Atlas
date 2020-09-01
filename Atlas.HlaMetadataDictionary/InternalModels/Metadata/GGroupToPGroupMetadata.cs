using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface IGGroupToPGroupMetadata : ISerialisableHlaMetadata
    {
        SerialisedPGroup SerialisedPGroup { get; }
    }

    public class SerialisedPGroup
    {
        public string PGroup { get; set; }
    }

    internal class GGroupToPGroupMetadata : SerialisableHlaMetadata, IGGroupToPGroupMetadata
    {
        public SerialisedPGroup SerialisedPGroup { get; }
        public override object HlaInfoToSerialise => SerialisedPGroup;

        public GGroupToPGroupMetadata(Locus locus, string lookupName, string pGroup)
            : base(locus, lookupName, TypingMethod.Molecular)
        {
            SerialisedPGroup = new SerialisedPGroup {PGroup = pGroup};
        }
    }
}
