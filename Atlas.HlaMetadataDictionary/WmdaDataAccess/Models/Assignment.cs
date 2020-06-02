using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.HlaMetadataDictionary.WmdaDataAccess.Models
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
