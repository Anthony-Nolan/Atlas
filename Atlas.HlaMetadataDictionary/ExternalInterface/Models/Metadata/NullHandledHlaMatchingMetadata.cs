using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    /// <summary>
    /// Contextually dependent on the <see cref="IHlaMatchingMetadata"/> of a paired allele at the same locus.
    /// In the case of expressing alleles, this data will be identical to the equivalent <see cref="IHlaMatchingMetadata"/>
    /// For null-expressing alleles, minor tweaks are made for matching purposes, which are documented on a per-property basis. 
    /// </summary>
    public interface INullHandledHlaMatchingMetadata 
    {
        /// <summary>
        /// For expressing alleles - the allele's name as in <see cref="IHlaMetadata.LookupName"/>.
        /// For null alleles - a combination of the null allele's lookup name, and the lookup name of it's paired expressing allele.
        /// </summary>
        string LookupName { get; }

        /// <summary>
        /// For expressing alleles - the P-Groups corresponding to the allele, as in <see cref="IHlaMatchingMetadata.MatchingPGroups"/>.
        /// For null alleles - the P-Groups of the paired expressing allele.
        /// </summary>
        IList<string> MatchingPGroups { get; }
    }

    internal class NullHandledHlaMatchingMetadata : HlaMatchingMetadata, INullHandledHlaMatchingMetadata
    {
        /// <param name="original">
        /// The default <see cref="IHlaMatchingMetadata"/> details for this allele.
        /// If only this is provided, this class will be nothing more than a subset of its values.
        /// </param>
        /// <param name="mergedLookupName"><see cref="LookupName"/> for details on when this should be set.</param>
        /// <param name="mergedPGroups"><see cref="MatchingPGroups"/> for details on when this should be specified.</param>
        public NullHandledHlaMatchingMetadata(IHlaMatchingMetadata original, string mergedLookupName = null, IList<string> mergedPGroups = null) :
            base(original.Locus, original.LookupName, original.TypingMethod, original.MatchingPGroups)
        {
            LookupName = mergedLookupName ?? original.LookupName;
            MatchingPGroups = mergedPGroups ?? original.MatchingPGroups;
        }

        public new string LookupName { get; set; }

        public new IList<string> MatchingPGroups { get; set; }
    }
}