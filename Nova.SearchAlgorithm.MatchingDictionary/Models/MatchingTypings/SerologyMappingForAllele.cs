using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class SerologyMappingForAllele : IEquatable<SerologyMappingForAllele>
    {
        public SerologyTyping DirectSerology { get; }
        public Assignment Assignment { get; }
        public IEnumerable<SerologyMatchToAllele> AllMatchingSerology { get; }
        
        public SerologyMappingForAllele(SerologyTyping directSerology, Assignment assignment, IEnumerable<SerologyMatchToAllele> allMatchingSerology)
        {
            DirectSerology = directSerology;
            Assignment = assignment;
            AllMatchingSerology = allMatchingSerology;
        }
        
        public bool Equals(SerologyMappingForAllele other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                DirectSerology.Equals(other.DirectSerology) 
                && Assignment == other.Assignment 
                && AllMatchingSerology.SequenceEqual(other.AllMatchingSerology);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyMappingForAllele) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DirectSerology.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Assignment;
                hashCode = (hashCode * 397) ^ AllMatchingSerology.GetHashCode();
                return hashCode;
            }
        }
    }
}
