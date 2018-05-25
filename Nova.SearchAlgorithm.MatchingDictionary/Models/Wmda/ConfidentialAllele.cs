using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class ConfidentialAllele : IWmdaHlaTyping, IEquatable<IWmdaHlaTyping>
    {
        public string WmdaLocus { get; }
        public string Name { get; }

        public ConfidentialAllele(string wmdaLocus, string name)
        {
            WmdaLocus = wmdaLocus;
            Name = name;
        }

        public override string ToString()
        {
            return $"locus: {WmdaLocus}, name: {Name}";
        }

        public bool Equals(IWmdaHlaTyping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(WmdaLocus, other.WmdaLocus) 
                && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is IWmdaHlaTyping other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (WmdaLocus.GetHashCode() * 397) ^ Name.GetHashCode();
            }
        }
    }
}
