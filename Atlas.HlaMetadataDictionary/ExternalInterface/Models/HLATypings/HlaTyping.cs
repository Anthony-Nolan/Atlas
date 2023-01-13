using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings
{
    public class HlaTyping : IEquatable<HlaTyping>, IWmdaHlaTyping
    {
        public TypingMethod TypingMethod { get; }
        public string TypingLocus { get; set; }
        public Locus Locus { get; }
        public string Name { get; set; }
        public bool IsDeleted { get; }

        internal HlaTyping(TypingMethod typingMethod, string typingLocus, string name, bool isDeleted = false)
        {
            TypingLocus = typingLocus;
            Name = name;
            TypingMethod = typingMethod;
            IsDeleted = isDeleted;
            Locus = HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(typingMethod, typingLocus);
        }

        public override string ToString()
        {
            return $"{Locus}{Name}";
        }

        #region IEquatable
        public bool Equals(HlaTyping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                TypingMethod == other.TypingMethod && 
                string.Equals(Locus, other.Locus) && 
                Locus == other.Locus && 
                string.Equals(Name, other.Name) && 
                IsDeleted == other.IsDeleted;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaTyping) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) TypingMethod;
                hashCode = (hashCode * 397) ^ Locus.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Locus;
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDeleted.GetHashCode();
                return hashCode;
            }
        }
        #endregion
    }
}
