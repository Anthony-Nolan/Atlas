using System.Collections.Generic;
using Newtonsoft.Json;

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

    /// <summary>
    /// Mapping summary for a given serology typing.
    /// </summary>
    internal class SerologyToAlleleMappingSummary
    {
        /// <summary>
        /// Name of serology typing(s) that match the given serology typing AND map to <see cref="Alleles"/>.
        /// </summary>
        [JsonProperty("ser")]
        public List<string> SerologyBridge { get; set; }

        /// <summary>
        /// Alleles mapped to serologies listed in <see cref="SerologyBridge"/>
        /// </summary>
        public List<string> Alleles { get; set; }

        /// <summary>
        /// G groups that <see cref="Alleles"/> belongs to
        /// </summary>
        [JsonProperty("gGrps")]
        public List<string> GGroups { get; set; }

        /// <summary>
        /// Name of P group that <see cref="Alleles"/> belongs to
        /// </summary>
        [JsonProperty("pGrp")]
        public string PGroup { get; set; }
    }
}