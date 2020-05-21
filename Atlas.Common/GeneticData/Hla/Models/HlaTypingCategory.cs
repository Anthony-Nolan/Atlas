using System;
using EnumStringValues;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Common.GeneticData.Hla.Models
{
    internal class TargetedFlexibleEnumConverter<TEnum> : StringEnumConverter where TEnum : Enum
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonSerializationException e)
            {
                if (e.Message.StartsWith("Error converting value"))
                {
                    try
                    {
                        return reader.Value.ToString().ParseToEnum<TEnum>();
                    }
                    catch (Exception)
                    {
                        // Do nothing and allow the previous Exception to be throw.
                    }
                }

                throw;
            }
        }
    }

    [JsonConverter(typeof(TargetedFlexibleEnumConverter<HlaTypingCategory>))]
    public enum HlaTypingCategory
    {
        /// <summary>
        /// Typed as the fully-qualified Allele
        /// </summary>
        [StringValue(nameof(Allele), true), StringValue("OriginalAllele")]
        Allele,

        /// <summary>
        /// Typed as a P-Group
        /// </summary>
        GGroup,

        /// <summary>
        /// Typed as a G-Group
        /// </summary>
        PGroup,

        /// <summary>
        /// Typed as a string of arbitrary alleles e.g. 01:02/01:03/02:04
        /// </summary>
        [StringValue(nameof(AlleleStringOfNames), true), StringValue("MultipleAlleles")]
        AlleleStringOfNames,

        /// <summary>
        /// Typed as a string of subtypes with common first field. e.g. 01:02/03/04
        /// </summary>
        AlleleStringOfSubtypes,

        /// <summary>
        /// Typed as the compressed allele name, as used in NMDP codes.
        /// </summary>
        [StringValue(nameof(NmdpCode), true), StringValue("NmdpCodeAllele")]
        NmdpCode,

        /// <summary>
        /// Typed as the XX code first field value.
        /// </summary>
        XxCode,

        /// <summary>
        /// Typed as the original serology name
        /// </summary>
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
