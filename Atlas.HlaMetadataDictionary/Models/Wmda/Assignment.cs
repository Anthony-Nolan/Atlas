using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
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
