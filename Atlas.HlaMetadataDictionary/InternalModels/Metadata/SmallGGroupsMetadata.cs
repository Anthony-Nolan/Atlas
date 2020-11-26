using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface ISmallGGroupsMetadata : ISerialisableHlaMetadata
    {
        IReadOnlyCollection<string> SmallGGroups { get; }
    }

    internal class SmallGGroupsMetadata : SerialisableHlaMetadata, ISmallGGroupsMetadata
    {
        public IReadOnlyCollection<string> SmallGGroups { get; }
        public override object HlaInfoToSerialise => SmallGGroups;

        public SmallGGroupsMetadata(Locus locus, string alleleName, string smallGGroup)
            : base(locus, alleleName, TypingMethod.Molecular)
        {
            SmallGGroups = new[] { smallGGroup };
        }

        public SmallGGroupsMetadata(Locus locus, string alleleName, IReadOnlyCollection<string> smallGGroups)
            : base(locus, alleleName, TypingMethod.Molecular)
        {
            SmallGGroups = smallGGroups;
        }
    }
}