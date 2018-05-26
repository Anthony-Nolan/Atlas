using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class RelSerSer : IWmdaHlaTyping, IEquatable<RelSerSer>
    {
        public string WmdaLocus { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> SplitAntigens { get; }
        public IEnumerable<string> AssociatedAntigens { get; }

        public RelSerSer(string wmdaLocus, string name, IEnumerable<string> splitAntigens, IEnumerable<string> associatedAntigens)
        {
            WmdaLocus = wmdaLocus;
            Name = name;
            SplitAntigens = splitAntigens;
            AssociatedAntigens = associatedAntigens;
        }

        public override string ToString()
        {
            return $"locus: {WmdaLocus}, name: {Name}, splits: {string.Join("/", SplitAntigens)}, associated: {string.Join("/", AssociatedAntigens)}";
        }

        public bool Equals(RelSerSer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(WmdaLocus, other.WmdaLocus) 
                && string.Equals(Name, other.Name) 
                && SplitAntigens.SequenceEqual(other.SplitAntigens) 
                && AssociatedAntigens.SequenceEqual(other.AssociatedAntigens);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RelSerSer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = WmdaLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ SplitAntigens.GetHashCode();
                hashCode = (hashCode * 397) ^ AssociatedAntigens.GetHashCode();
                return hashCode;
            }
        }
    }
}
