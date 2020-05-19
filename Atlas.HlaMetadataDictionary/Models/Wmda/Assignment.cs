using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum Assignment
    {
        Unambiguous,
        Possible,
        Assumed,
        Expert,
        None
    }
}
