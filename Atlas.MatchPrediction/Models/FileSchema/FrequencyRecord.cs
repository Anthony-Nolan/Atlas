using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Models.FileSchema
{
    public class FrequencyRecord
    {
        [JsonProperty(Required = Required.Always)]
        public string A { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string B { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string C { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Dqb1 { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Drb1 { get; set; }

        [JsonProperty(Required = Required.Always)]
        public decimal Frequency { get; set; }
    }
}
