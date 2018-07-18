using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LookupCategory
    {
        /// <summary>
        /// Lookup original serology name, as listed in hla_nom.
        /// </summary>
        Serology,

        /// <summary>
        /// Lookup original allele name, as listed in hla_nom.
        /// </summary>
        OriginalAllele,

        /// <summary>
        /// Lookup truncated allele name, as used in NMDP codes.
        /// </summary>
        NmdpCodeAllele,

        /// <summary>
        /// Lookup XX code's first field value.
        /// </summary>
        XxCode
    }
}
