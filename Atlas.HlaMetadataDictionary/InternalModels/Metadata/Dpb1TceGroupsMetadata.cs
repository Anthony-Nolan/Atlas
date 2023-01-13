using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface IDpb1TceGroupsMetadata : ISerialisableHlaMetadata
    {
        string TceGroup { get; }
    }

    internal class Dpb1TceGroupsMetadata : SerialisableHlaMetadata, IDpb1TceGroupsMetadata
    {
        public string TceGroup { get; }
        public override object HlaInfoToSerialise => TceGroup;

        public Dpb1TceGroupsMetadata( string lookupName, string tceGroup)
            : base(Locus.Dpb1, lookupName, TypingMethod.Molecular)
        {
            TceGroup = tceGroup;
        }
    }
}
