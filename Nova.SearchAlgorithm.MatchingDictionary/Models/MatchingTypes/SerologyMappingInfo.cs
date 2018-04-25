using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public class SerologyMappingInfo : IEquatable<SerologyMappingInfo>
    {
        public Serology DirectSerology { get; }
        public Assignment Assignment { get; }
        public IEnumerable<SerologyMatchInfo> AllMatchingSerology { get; }
        
        public SerologyMappingInfo(Serology directSerology, Assignment assignment, IEnumerable<SerologyMatchInfo> allMatchingSerology)
        {
            DirectSerology = directSerology;
            Assignment = assignment;
            AllMatchingSerology = allMatchingSerology;
        }
        
        public bool Equals(SerologyMappingInfo other)
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
            return Equals((SerologyMappingInfo) obj);
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
