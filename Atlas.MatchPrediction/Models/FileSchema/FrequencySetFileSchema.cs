using System.Collections.Generic;
using Newtonsoft.Json;

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
    }
}
