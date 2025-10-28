using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Common.GeneticData.Hla.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HlaTypingCategory
    {
        /// <summary>
        /// Typed as the fully-qualified Allele
        /// </summary>
        Allele,

        /// <summary>
        /// Typed as a P-Group
        /// </summary>
        GGroup,
        
        /// <summary>
        /// Typed as a "small g" group
        /// </summary>
        SmallGGroup,

        /// <summary>
        /// Typed as a G-Group
        /// </summary>
        PGroup,

        /// <summary>
        /// Typed as a string of arbitrary alleles e.g. 01:02/01:03/02:04
        /// </summary>
        AlleleStringOfNames,

        /// <summary>
        /// Typed as a string of subtypes with common first field. e.g. 01:02/03/04
        /// </summary>
        AlleleStringOfSubtypes,

        /// <summary>
        /// Typed as the compressed allele name, as used in NMDP codes.
        /// </summary>
        NmdpCode,

        /// <summary>
        /// Typed as the XX code first field value.
        /// </summary>
        XxCode,

        /// <summary>
        /// Typed as the original serology name
        /// </summary>
        Serology,

        /// <summary>
        /// Typed as an allele not yet assigned a name
        /// </summary>
        NEW
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypingMethod
    {
        Molecular,
        Serology
    }

    public static class TypingsExtension
    {
        public static TypingMethod ToTypingMethod(this HlaTypingCategory category)
        {
            if (category == HlaTypingCategory.Serology)
            {
                return TypingMethod.Serology;
            }

            return TypingMethod.Molecular;
        }
    }
}
