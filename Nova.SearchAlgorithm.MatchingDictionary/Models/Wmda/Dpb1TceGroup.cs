using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class Dpb1TceGroup : IWmdaHlaTyping, IEquatable<Dpb1TceGroup>
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;

        /// <summary>
        /// Locus will always be DPB1*
        /// </summary>
        public string Locus
        {
            get => "DPB1*";
            set {  }
        }

        /// <summary>
        /// DPB1 Allele Name
        /// </summary>
        public string Name { get; set; }

        public string ProteinName { get; }
        public string VersionOneAssignment { get; }
        public string VersionTwoAssignment { get; }

        public Dpb1TceGroup(
            string alleleName,
            string proteinName, 
            string versionOneAssignment, 
            string versionTwoAssignment)
        {
            Name = alleleName;
            ProteinName = proteinName;
            VersionOneAssignment = versionOneAssignment;
            VersionTwoAssignment = versionTwoAssignment;
        }

        public bool Equals(Dpb1TceGroup other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(Locus, other.Locus) && 
                string.Equals(Name, other.Name) && 
                string.Equals(ProteinName, other.ProteinName) && 
                string.Equals(VersionOneAssignment, other.VersionOneAssignment) && 
                string.Equals(VersionTwoAssignment, other.VersionTwoAssignment);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Dpb1TceGroup) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Locus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ ProteinName.GetHashCode();
                hashCode = (hashCode * 397) ^ VersionOneAssignment.GetHashCode();
                hashCode = (hashCode * 397) ^ VersionTwoAssignment.GetHashCode();
                return hashCode;
            }
        }
    }
}
