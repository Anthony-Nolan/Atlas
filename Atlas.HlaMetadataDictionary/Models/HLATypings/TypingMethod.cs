using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypingMethod
    {
        Molecular,
        Serology
    }
}
