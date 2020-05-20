using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.HlaMetadataDictionary.Models.Lookups
{
    ///QQ Combine with <see cref="Common.GeneticData.Hla.Models.HlaTypingCategory"/>
    /// Check for usages of strings!
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LookupNameCategory
    {
        /// <summary>
        /// Lookup name is original serology name, as listed in hla_nom.
        /// </summary>
        Serology,

        /// <summary>
        /// Lookup name is original allele name, as listed in hla_nom.
        /// </summary>
        OriginalAllele,

        /// <summary>
        /// Lookup name is truncated allele name, as used in NMDP codes.
        /// </summary>
        NmdpCodeAllele,

        /// <summary>
        /// Lookup name is XX code first field value.
        /// </summary>
        XxCode,

        /// <summary>
        /// Lookup name represents multiple alleles. 
        /// </summary>
        MultipleAlleles //QQCareful with this!
    }
}
