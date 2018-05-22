using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class RelDnaSerMapping : IEquatable<RelDnaSerMapping>
    {
        public SerologyTyping DirectSerology { get; }
        public Assignment Assignment { get; }
        public IEnumerable<RelDnaSerMatch> AllMatchingSerology { get; }
        
        public RelDnaSerMapping(SerologyTyping directSerology, Assignment assignment, IEnumerable<RelDnaSerMatch> allMatchingSerology)
        {
            DirectSerology = directSerology;
            Assignment = assignment;
            AllMatchingSerology = allMatchingSerology;
        }
        
        public bool Equals(RelDnaSerMapping other)
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
            return Equals((RelDnaSerMapping) obj);
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
