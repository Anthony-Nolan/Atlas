using System.Collections.Generic;
using Atlas.MatchPrediction.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Models
{
    // ReSharper disable InconsistentNaming

    /// <summary>
    /// File schema used to add attributes only needed for serialisation in tests.
    /// Will be deserialised to <see cref="FrequencySetFileSchema"/>
    /// </summary>
    internal class SerialisableFrequencySetFileContents
    {
        /// <see cref="FrequencySetFileSchema.HlaNomenclatureVersion"/>
        [JsonProperty(Order = 1)]
        public string nomenclatureVersion { get; set; }

        /// <see cref="FrequencySetFileSchema.RegistryCodes"/>
        [JsonProperty(Order = 2)]
        public string[] donPool { get; set; }

        /// <see cref="FrequencySetFileSchema.EthnicityCodes"/>
        [JsonProperty(Order = 3)]
        public string[] ethn { get; set; }

        /// <see cref="FrequencySetFileSchema.PopulationId"/>
        [JsonProperty(Order = 4)]
        public int populationId { get; set; }

        /// <see cref="FrequencySetFileSchema.TypingCategory"/>
        [JsonProperty(Order = 5)]
        public ImportTypingCategory? TypingCategory { get; set; } = ImportTypingCategory.LargeGGroup;

        /// <see cref="FrequencySetFileSchema.Frequencies"/>
        [JsonProperty(Order = 6)]
        public IEnumerable<FrequencyRecord> frequencies { get; set; }
    }
}