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
        /// Name of serology typing that matches the given serology typing AND map to <see cref="Alleles"/>.
        /// </summary>
        [JsonProperty("ser")]
        public string SerologyBridge { get; set; }

        /// <summary>
        /// Name of P group that <see cref="Alleles"/> belong to
        /// </summary>
        [JsonProperty("pGrp")]
        public string PGroup { get; set; }

        /// <summary>
        /// Alleles mapped to serologies listed in <see cref="SerologyBridge"/>
        /// </summary>
        [JsonProperty("alleles")]
        public List<string> Alleles { get; set; }

        /// <summary>
        /// G groups that <see cref="Alleles"/> belongs to
        /// </summary>
        [JsonProperty("gGrps")]
        public List<string> GGroups { get; set; }
    }
}
