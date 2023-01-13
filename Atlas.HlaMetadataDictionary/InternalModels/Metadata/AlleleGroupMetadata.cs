using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface IAlleleGroupMetadata : ISerialisableHlaMetadata
    {
        List<string> AllelesInGroup { get; }
    }

    internal class AlleleGroupMetadata : SerialisableHlaMetadata, IAlleleGroupMetadata
    {
        public override object HlaInfoToSerialise => AllelesInGroup.ToList(); //Needs to be reified for deserialisation Type validation;
        public List<string> AllelesInGroup { get; }

        public AlleleGroupMetadata(Locus locus, string alleleGroupName, IEnumerable<string> allelesInGroup)
            : base(locus, alleleGroupName, TypingMethod.Molecular)
        {
            AllelesInGroup = allelesInGroup.ToList();
        }
    }
}
