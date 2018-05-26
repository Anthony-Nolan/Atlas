using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public class HlaTyping : IEquatable<HlaTyping>, IWmdaHlaTyping
    {
        public string WmdaLocus { get; set; }
        public MatchLocus MatchLocus { get; }
        public string Name { get; set; }
        public bool IsDeleted { get; }

        public HlaTyping(string wmdaLocus, string name, bool isDeleted = false)
        {
            WmdaLocus = wmdaLocus;
            Name = name;
            IsDeleted = isDeleted;
            MatchLocus = SetMatchLocus(wmdaLocus, name);
        }

        public override string ToString()
        {
            return $"{WmdaLocus}{Name}";
        }       

        public bool Equals(HlaTyping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(WmdaLocus, other.WmdaLocus) && 
                MatchLocus == other.MatchLocus && 
                string.Equals(Name, other.Name) && 
                IsDeleted == other.IsDeleted;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaTyping) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = WmdaLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) MatchLocus;
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDeleted.GetHashCode();
                return hashCode;
            }
        }

        protected static MatchLocus SetMatchLocus(string wmdaLocus, string name)
        {
            if (wmdaLocus.Equals("DR") && Drb345Serologies.Drb345Typings.Contains(name))
                throw new ArgumentException($"{name} is part of DRB345, not DRB1.");

            return LocusNames.GetMatchLocusFromWmdaLocus(wmdaLocus);
        }
    }
}
