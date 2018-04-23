using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Subtype
    {
        NotSplit,
        Broad,
        Split,
        Associated
    }

    public class Serology : HlaType, IEquatable<Serology>
    {
        public Subtype Subtype { get; }

        public Serology(Serology serology) 
            : this(serology.WmdaLocus, serology.Name, serology.Subtype, serology.IsDeleted)
        {
        }

        public Serology(IWmdaHlaType hlaType, Subtype subtype, bool isDeleted = false) 
            : this(hlaType.WmdaLocus, hlaType.Name, subtype, isDeleted)
        {
        }

        [JsonConstructor]
        public Serology(string wmdaLocus, string name, Subtype subtype, bool isDeleted = false) 
            : base(wmdaLocus, name, isDeleted)
        {
            Subtype = subtype;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, subtype: {Subtype}";
        }

        public bool Equals(Serology other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Subtype == other.Subtype;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Serology) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (int) Subtype;
            }
        }
    }
}
