using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchPrediction.Models.FileSchema
{
    public class FrequencySetFileSchema
    {
        [JsonProperty(PropertyName = "nomenclatureVersion", Required = Required.Always)]
        public string HlaNomenclatureVersion { get; set; }

        [JsonProperty(PropertyName = "donPool")]
        public string[] RegistryCodes { get; set; }

        // ReSharper disable once StringLiteralTypo
        [JsonProperty(PropertyName = "ethn")]
        public string[] EthnicityCodes { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int PopulationId { get; set; }

        [JsonProperty(Required = Required.Always)]
        public IEnumerable<FrequencyRecord> Frequencies { get; set; }

        /// <summary>
        /// Haplotype frequency sets are support at multiple resolutions - the resolutions within a given set must be consistent.
        /// </summary>
        [JsonConverter(typeof(ImportTypingCategoryStringEnumConverter))]
        public ImportTypingCategory? TypingCategory { get; set; }
    }

    public class ImportTypingCategoryStringEnumConverter : StringEnumConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ImportTypingCategory) || objectType == typeof(ImportTypingCategory?);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
            {
                return null;
            }

            var enumString = (reader.Value?.ToString() ?? string.Empty).Trim().ToLower();
            return enumString switch
            {
                "small_g" => ImportTypingCategory.SmallGGroup,
                "large_g" => ImportTypingCategory.LargeGGroup,
                _ => base.ReadJson(reader, objectType, existingValue, serializer)
            };
        }
    }
}