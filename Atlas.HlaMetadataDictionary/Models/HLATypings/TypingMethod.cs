using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.HlaMetadataDictionary.Models.HLATypings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypingMethod
    {
        Molecular,
        Serology
    }
}
