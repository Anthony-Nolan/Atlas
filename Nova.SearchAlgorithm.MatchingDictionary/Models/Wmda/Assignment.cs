using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Assignment
    {
        Unambiguous,
        Possible,
        Assumed,
        Expert,
        None
    }
}
