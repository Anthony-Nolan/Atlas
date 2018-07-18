using System;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public class HlaScoringLookupResult : 
        IHlaScoringLookupResult, 
        IEquatable<HlaScoringLookupResult>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public HlaTypingCategory HlaTypingCategory { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            HlaTypingCategory hlaTypingCategory,
            IHlaScoringInfo hlaScoringInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaTypingCategory = hlaTypingCategory;
            HlaScoringInfo = hlaScoringInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }

        public bool Equals(HlaScoringLookupResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                MatchLocus == other.MatchLocus && 
                string.Equals(LookupName, other.LookupName) && 
                TypingMethod == other.TypingMethod && 
                HlaTypingCategory == other.HlaTypingCategory && 
                HlaScoringInfo.Equals(other.HlaScoringInfo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaScoringLookupResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) MatchLocus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) TypingMethod;
                hashCode = (hashCode * 397) ^ (int) HlaTypingCategory;
                hashCode = (hashCode * 397) ^ HlaScoringInfo.GetHashCode();
                return hashCode;
            }
        }
    }
}
