using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    /// <summary>
    /// Metadata for a molecular HLA typing known to only have a single P group, e.g., G group, small g group.
    /// </summary>
    internal interface IMolecularTypingToPGroupMetadata : ISerialisableHlaMetadata
    {
        /// <summary>
        /// Will be an empty string where molecular typing is non-expressing and has no corresponding P group.
        /// </summary>
        public string PGroup { get; }
    }

    internal class MolecularTypingToPGroupMetadata : SerialisableHlaMetadata, IMolecularTypingToPGroupMetadata
    {
        public string PGroup { get; }
        public override object HlaInfoToSerialise => PGroup;

        public MolecularTypingToPGroupMetadata(Locus locus, string lookupName, string pGroup)
            : base(locus, lookupName, TypingMethod.Molecular)
        {
            PGroup = pGroup ?? string.Empty;
        }
    }
}