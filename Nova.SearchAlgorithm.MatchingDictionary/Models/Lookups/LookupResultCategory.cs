using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LookupResultCategory
    {
        /// <summary>
        /// Lookup result is original serology name, as listed in hla_nom.
        /// </summary>
        Serology,

        /// <summary>
        /// Lookup result is original allele name, as listed in hla_nom.
        /// </summary>
        OriginalAllele,

        /// <summary>
        /// Lookup result is truncated allele name, as used in NMDP codes.
        /// </summary>
        NmdpCodeAllele,

        /// <summary>
        /// Lookup result is XX code first field value.
        /// </summary>
        XxCode,

        /// <summary>
        /// Lookup result is collection of multiple alleles. 
        /// </summary>
        MultipleAlleles
    }
}
