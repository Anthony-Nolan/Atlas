using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public class AlleleScoringLookupResult : 
        IHlaScoringLookupResult,
        IStorableInCloudTable<HlaLookupTableEntity>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public AlleleTypingStatus AlleleTypingStatus { get; }
        public string MatchingPGroup { get; }
        public string MatchingGGroup { get; }

        /// <summary>
        /// Matching serologies used when scoring the allele against a serology typing.
        /// </summary>
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonConstructor]
        public AlleleScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            AlleleTypingStatus alleleTypingStatus,
            string matchingPGroup,
            string matchingGGroup,
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            AlleleTypingStatus = alleleTypingStatus;
            MatchingPGroup = matchingPGroup;
            MatchingGGroup = matchingGGroup;
            MatchingSerologies = matchingSerologies;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
    }
}
