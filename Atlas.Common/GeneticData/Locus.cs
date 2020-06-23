using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Common.GeneticData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Locus
    {
        A,
        B,
        C,
        Dpb1,
        Dqb1,
        Drb1
    }
}
