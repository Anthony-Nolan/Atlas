using System;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda
{
    public class Dpb1TceGroupAssignment : IWmdaHlaTyping, IEquatable<Dpb1TceGroupAssignment>
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;

        /// <summary>
        /// Locus will always be DPB1*
        /// </summary>
        public string TypingLocus
        {
            get => "DPB1*";
            set {  }
        }

        /// <summary>
        /// DPB1 Allele Name
        /// </summary>
        public string Name { get; set; }

        public string VersionOneAssignment { get; }
        public string VersionTwoAssignment { get; }

        public Dpb1TceGroupAssignment(
            string alleleName,
            string versionOneAssignment, 
            string versionTwoAssignment)
        {
            Name = alleleName;
            VersionOneAssignment = versionOneAssignment;
            VersionTwoAssignment = versionTwoAssignment;
        }

        public bool Equals(Dpb1TceGroupAssignment other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(TypingLocus, other.TypingLocus) && 
                string.Equals(Name, other.Name) &&
                string.Equals(VersionOneAssignment, other.VersionOneAssignment) && 
                string.Equals(VersionTwoAssignment, other.VersionTwoAssignment);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Dpb1TceGroupAssignment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TypingLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ VersionOneAssignment.GetHashCode();
                hashCode = (hashCode * 397) ^ VersionTwoAssignment.GetHashCode();
                return hashCode;
            }
        }
    }
}
