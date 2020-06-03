using Atlas.Common.GeneticData.Hla.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    internal class RelSerSer : IWmdaHlaTyping, IEquatable<RelSerSer>
    {
        public TypingMethod TypingMethod => TypingMethod.Serology;
        public string TypingLocus { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> SplitAntigens { get; }
        public IEnumerable<string> AssociatedAntigens { get; }

        public RelSerSer(string locus, string name, IEnumerable<string> splitAntigens, IEnumerable<string> associatedAntigens)
        {
            TypingLocus = locus;
            Name = name;
            SplitAntigens = splitAntigens;
            AssociatedAntigens = associatedAntigens;
        }

        public override string ToString()
        {
            return $"locus: {TypingLocus}, name: {Name}, splits: {string.Join("/", SplitAntigens)}, associated: {string.Join("/", AssociatedAntigens)}";
        }


        public bool Equals(RelSerSer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(TypingLocus, other.TypingLocus) && 
                string.Equals(Name, other.Name) && 
                SplitAntigens.SequenceEqual(other.SplitAntigens) && 
                AssociatedAntigens.SequenceEqual(other.AssociatedAntigens);
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
                var hashCode = TypingLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ SplitAntigens.GetHashCode();
                hashCode = (hashCode * 397) ^ AssociatedAntigens.GetHashCode();
                return hashCode;
            }
        }
    }
}
