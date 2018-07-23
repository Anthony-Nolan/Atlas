using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public class HlaScoringLookupResult : 
        IHlaScoringLookupResult, 
        IEquatable<HlaScoringLookupResult>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }

        public TypingMethod TypingMethod => LookupResultCategory == LookupResultCategory.Serology
            ? TypingMethod.Serology
            : TypingMethod.Molecular;

        public LookupResultCategory LookupResultCategory { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }
        public object HlaInfoToSerialise => HlaScoringInfo;

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            LookupResultCategory lookupResultCategory,
            IHlaScoringInfo hlaScoringInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            LookupResultCategory = lookupResultCategory;
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
                LookupResultCategory == other.LookupResultCategory && 
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
                hashCode = (hashCode * 397) ^ (int) LookupResultCategory;
                hashCode = (hashCode * 397) ^ HlaScoringInfo.GetHashCode();
                return hashCode;
            }
        }
    }
}
