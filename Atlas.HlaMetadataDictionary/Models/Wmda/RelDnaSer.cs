using Atlas.Common.GeneticData.Hla.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    public class RelDnaSer : IWmdaHlaTyping, IEquatable<RelDnaSer>
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TypingLocus { get; set; }
        public string Name { get; set; }
        public IEnumerable<SerologyAssignment> Assignments { get; }
        public IEnumerable<string> Serologies { get; }

        public RelDnaSer(string locus, string name, IEnumerable<SerologyAssignment> assignments)
        {
            TypingLocus = locus;
            Name = name;
            Assignments = assignments;
            Serologies = Assignments.Select(a => a.Name).Distinct().OrderBy(s => s);
        }

        public override string ToString()
        {
            return $"locus: {TypingLocus}, allele: {Name}, assignments: {string.Join("/", Assignments)}";
        }

        public bool Equals(RelDnaSer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(TypingLocus, other.TypingLocus) 
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
                var hashCode = TypingLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Assignments.GetHashCode();
                return hashCode;
            }
        }
    }
}
