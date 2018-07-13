using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public class AlleleScoringLookupResult : 
        IHlaScoringLookupResult,
        IStorableInCloudTable<HlaLookupTableEntity>,
        IEquatable<AlleleScoringLookupResult>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public AlleleTypingStatus AlleleTypingStatus { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }

        /// <summary>
        /// Matching Serologies need when scoring the allele against a serology typing.
        /// </summary>
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonConstructor]
        public AlleleScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            AlleleTypingStatus alleleTypingStatus,
            IEnumerable<string> matchingPGroups,
            IEnumerable<string> matchingGGroups,
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            AlleleTypingStatus = alleleTypingStatus;
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchingSerologies;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }

        public bool Equals(AlleleScoringLookupResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                MatchLocus == other.MatchLocus && 
                string.Equals(LookupName, other.LookupName) && 
                AlleleTypingStatus.Equals(other.AlleleTypingStatus) && 
                MatchingPGroups.SequenceEqual(other.MatchingPGroups) && 
                MatchingGGroups.SequenceEqual(other.MatchingGGroups) && 
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlleleScoringLookupResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) MatchLocus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ AlleleTypingStatus.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingGGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
