using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SerologySubtype
    {
        NotSerologyTyping = 0,
        NotSplit = 1,
        Broad = 2,
        Split = 3,
        Associated = 4
    }
}
