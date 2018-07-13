using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public class SerologyScoringLookupResult : 
        IHlaScoringLookupResult,
        IStorableInCloudTable<HlaLookupTableEntity>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Serology;
        public SerologySubtype SerologySubtype { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonConstructor]
        public SerologyScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            SerologySubtype serologySubtype,
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            SerologySubtype = serologySubtype;
            MatchingSerologies = matchingSerologies;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
    }
}
