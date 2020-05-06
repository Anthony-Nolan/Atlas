using Atlas.HlaMetadataDictionary.Models.HLATypings;
using System;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    public class AlleleStatus : IWmdaHlaTyping, IEquatable<AlleleStatus>
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TypingLocus { get; set; }
        public string Name { get; set; }
        public string SequenceStatus { get; }
        public string DnaCategory { get; }

        public AlleleStatus(string locus, string name, string sequenceStatus, string dnaCategory)
        {
            TypingLocus = locus;
            Name = name;
            SequenceStatus = sequenceStatus;
            DnaCategory = dnaCategory;
        }

        public override string ToString()
        {
            return $"locus: {TypingLocus}, name: {Name}";
        }
        
        public bool Equals(AlleleStatus other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(TypingLocus, other.TypingLocus) && 
                string.Equals(Name, other.Name) && 
                string.Equals(SequenceStatus, other.SequenceStatus) && 
                string.Equals(DnaCategory, other.DnaCategory);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlleleStatus) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TypingLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ SequenceStatus.GetHashCode();
                hashCode = (hashCode * 397) ^ DnaCategory.GetHashCode();
                return hashCode;
            }
        }
    }
}
