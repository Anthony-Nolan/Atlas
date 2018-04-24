using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class RelDnaSer : IWmdaHlaType, IEquatable<RelDnaSer>
    {
        public string WmdaLocus { get; }
        public string Name { get; }
        public IEnumerable<SerologyAssignment> Assignments { get; }
        public IEnumerable<string> Serologies { get; }

        public RelDnaSer(string wmdaLocus, string name, IEnumerable<SerologyAssignment> assignments)
        {
            WmdaLocus = wmdaLocus;
            Name = name;
            Assignments = assignments;
            Serologies = Assignments.Select(a => a.Name).Distinct().OrderBy(s => s);
        }

        public override string ToString()
        {
            return $"locus: {WmdaLocus}, allele: {Name}, assignments: {string.Join("/", Assignments)}";
        }

        public bool Equals(RelDnaSer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(WmdaLocus, other.WmdaLocus) 
                && string.Equals(Name, other.Name) 
                && Assignments.SequenceEqual(other.Assignments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RelDnaSer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = WmdaLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Assignments.GetHashCode();
                return hashCode;
            }
        }
    }
}
