using System.Collections.Generic;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Models
{
    public class FrequencySetFileSchema
    {
        // ReSharper disable once StringLiteralTypo
        [JsonProperty(PropertyName = "ethn")]
        public string Ethnicity { get; set; }

        [JsonProperty(PropertyName = "donPool")]
        public string[] RegistryCodes { get; set; }

        [JsonProperty(PropertyName = "nomenclatureVersion")]
        public string NomenclatureVersion { get; set; }

        [JsonProperty(PropertyName = "populationId")]
        public int PopulationId { get; set; }

        [JsonProperty(PropertyName = "frequencies")]
        public IEnumerable<FrequencyRecord> Frequencies { get; set; }
    }
}
