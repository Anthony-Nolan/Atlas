using Newtonsoft.Json;
using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    /// <summary>
    /// Mapping summary for a given serology typing.
    /// </summary>
    public class SerologyToAlleleMappingSummary
    {
        /// <summary>
        /// Name of single serology typing that matches the given serology typing AND maps to <see cref="Alleles"/>.
        /// </summary>
        [JsonProperty("ser")]
        public string SerologyBridge { get; set; }

        /// <summary>
        /// Name of single P group that <see cref="Alleles"/> belong to
        /// </summary>
        [JsonProperty("pGrp")]
        public string PGroup { get; set; }

        /// <summary>
        /// Alleles mapped to serology listed in <see cref="SerologyBridge"/> and belong to the same <see cref="PGroup"/>
        /// </summary>
        [JsonProperty("alleles")]
        public List<string> Alleles { get; set; }

        /// <summary>
        /// G groups that <see cref="Alleles"/> belong to
        /// </summary>
        [JsonProperty("gGrps")]
        public List<string> GGroups { get; set; }
    }
}
