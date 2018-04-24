using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SerologySubtype
    {
        NotSerologyType = 0,
        NotSplit = 1,
        Broad = 2,
        Split = 3,
        Associated = 4
    }
}
