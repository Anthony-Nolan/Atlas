using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MolecularSubtype
    {
        NotMolecularTyping = 0,
        CompleteAllele = 1,
        TwoFieldAllele = 2,
        FirstFieldAllele = 3,
        MultipleAlleles = 4
    }
}
