using System;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes
{
    public class HlaType : IEquatable<HlaType>, IWmdaHlaType
    {
        public string WmdaLocus { get; }
        public string MatchLocus { get; }
        public string Name { get; }
        public bool IsDeleted { get; }

        public HlaType(string wmdaLocus, string hlaName, bool isDeleted = false)
        {
            WmdaLocus = wmdaLocus;
            Name = hlaName;
            IsDeleted = isDeleted;
            MatchLocus = SetMatchLocus(wmdaLocus, hlaName);
        }

        public override string ToString()
        {
            return $"{WmdaLocus}{Name}";
        }

        public bool Equals(HlaType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                string.Equals(WmdaLocus, other.WmdaLocus) &&
                string.Equals(Name, other.Name) &&
                IsDeleted == other.IsDeleted;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as HlaType;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = WmdaLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDeleted.GetHashCode();
                return hashCode;
            }
        }

        protected static string SetMatchLocus(string wmdaLocus, string hlaName)
        {
            if (wmdaLocus.Equals("DR") && Drb345Serologies.Drb345Types.Contains(hlaName))
                throw new ArgumentException($"{hlaName} is part of DRB345, not DRB1.");

            var matchLocus = LocusNames.GetMatchLocusFromWmdaLocus(wmdaLocus);

            if (matchLocus == null)
                throw new ArgumentException($"{wmdaLocus} is not a match locus");

            return matchLocus;
        }
    }
}
