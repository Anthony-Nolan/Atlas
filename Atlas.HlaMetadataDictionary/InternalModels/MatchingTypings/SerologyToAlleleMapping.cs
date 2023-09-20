using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    /// <summary>
    /// Mapping for a given serology typing.
    /// </summary>
    internal class SerologyToAlleleMapping
    {
        /// <summary>
        /// Allele that maps to given serology.
        /// </summary>
        public AlleleInfoForMatching MatchedAllele { get; set; }

        /// <summary>
        /// Name of serology typing(s) that match the given serology typing AND map to <see cref="MatchedAllele"/>.
        /// </summary>
        public IEnumerable<string> SerologyBridge { get; set; }
    }
}