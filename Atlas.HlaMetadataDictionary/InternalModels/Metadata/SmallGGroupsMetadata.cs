using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface ISmallGGroupsMetadata : ISerialisableHlaMetadata
    {
        List<string> SmallGGroups { get; }
    }

    internal class SmallGGroupsMetadata : SerialisableHlaMetadata, ISmallGGroupsMetadata
    {
        public List<string> SmallGGroups { get; }
        public override object HlaInfoToSerialise => SmallGGroups;

        public SmallGGroupsMetadata(Locus locus, string alleleName, TypingMethod typingMethod, string smallGGroup)
            : base(locus, alleleName, typingMethod)
        {
            SmallGGroups = new List<string> {smallGGroup};
        }

        public SmallGGroupsMetadata(Locus locus, string alleleName, TypingMethod typingMethod, List<string> smallGGroups)
            : base(locus, alleleName, typingMethod)
        {
            SmallGGroups = smallGGroups;
        }
    }
}