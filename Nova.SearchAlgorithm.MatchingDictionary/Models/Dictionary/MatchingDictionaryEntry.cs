using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypingMethod
    {
        Molecular,
        Serology
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MolecularSubtype
    {
        NotMolecularType = 0,
        CompleteAllele = 1,
        TwoFieldAllele = 2,
        FirstFieldAllele = 3
    }

    public class MatchingDictionaryEntry
    {
        public string MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public MolecularSubtype MolecularSubtype { get; }
        public SerologySubtype SerologySubtype { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<SerologyEntry> MatchingSerology { get; }

        public MatchingDictionaryEntry(
            string matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            MolecularSubtype molecularSubtype,
            SerologySubtype serologySubtype,
            IEnumerable<string> matchingPGroups,
            IEnumerable<SerologyEntry> matchingSerology
            )
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            MolecularSubtype = molecularSubtype;
            SerologySubtype = serologySubtype;
            MatchingPGroups = matchingPGroups;
            MatchingSerology = matchingSerology;
        }
    }
}
