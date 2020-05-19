using Atlas.Common.GeneticData.Hla.Models;
using System;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    internal class HlaNom : IWmdaHlaTyping, IEquatable<HlaNom>
    {
        public TypingMethod TypingMethod { get; }
        public string TypingLocus { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; }
        public string IdenticalHla { get; }

        public HlaNom(TypingMethod typingMethod, string locus, string name, bool isDeleted = false, string identicalHla = "")
        {
            TypingLocus = locus;
            Name = name;
            TypingMethod = typingMethod;
            IsDeleted = isDeleted;
            IdenticalHla = identicalHla;
        }

        public override string ToString()
        {
            return $"locus: {TypingLocus}, name: {Name}, deleted: {IsDeleted}, identicalHla: {IdenticalHla}";
        }


        public bool Equals(HlaNom other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                TypingMethod == other.TypingMethod && 
                string.Equals(TypingLocus, other.TypingLocus) && 
                string.Equals(Name, other.Name) && 
                IsDeleted == other.IsDeleted && 
                string.Equals(IdenticalHla, other.IdenticalHla);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaNom) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) TypingMethod;
                hashCode = (hashCode * 397) ^ TypingLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDeleted.GetHashCode();
                hashCode = (hashCode * 397) ^ IdenticalHla.GetHashCode();
                return hashCode;
            }
        }
    }
}
