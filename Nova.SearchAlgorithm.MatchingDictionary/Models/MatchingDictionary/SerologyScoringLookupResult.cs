using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public class SerologyScoringLookupResult : 
        IHlaScoringLookupResult,
        IStorableInCloudTable<HlaLookupTableEntity>,
        IEquatable<SerologyScoringLookupResult>
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

        public bool Equals(SerologyScoringLookupResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                MatchLocus == other.MatchLocus && 
                string.Equals(LookupName, other.LookupName) && 
                SerologySubtype == other.SerologySubtype && 
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyScoringLookupResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) MatchLocus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) SerologySubtype;
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
