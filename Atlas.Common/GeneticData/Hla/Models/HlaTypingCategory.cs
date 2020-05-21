using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Common.GeneticData.Hla.Models
{
    public enum HlaTypingCategory
    {
        Allele,
        GGroup,
        PGroup,
        AlleleStringOfNames,
        AlleleStringOfSubtypes,
        NmdpCode,
        XxCode,
        Serology
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
