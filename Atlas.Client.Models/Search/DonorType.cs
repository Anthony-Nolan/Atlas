// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Search
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DonorType
    {
        Adult = 1,
        Cord = 2
    }
}