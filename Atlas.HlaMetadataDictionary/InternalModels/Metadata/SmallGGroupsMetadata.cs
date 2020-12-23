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

        public SmallGGroupsMetadata(Locus locus, string alleleName, string smallGGroup)
            : base(locus, alleleName, TypingMethod.Molecular)
        {
            SmallGGroups = new List<string> {smallGGroup};
        }

        public SmallGGroupsMetadata(Locus locus, string alleleName, List<string> smallGGroups)
            : base(locus, alleleName, TypingMethod.Molecular)
        {
            SmallGGroups = smallGGroups;
        }
    }
}