using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes
{
    public class Serology : HlaType, IEquatable<Serology>
    {
        public SerologySubtype SerologySubtype { get; }

        public Serology(Serology serology) 
            : this(serology.WmdaLocus, serology.Name, serology.SerologySubtype, serology.IsDeleted)
        {
        }

        public Serology(IWmdaHlaType hlaType, SerologySubtype serologySubtype, bool isDeleted = false) 
            : this(hlaType.WmdaLocus, hlaType.Name, serologySubtype, isDeleted)
        {
        }

        [JsonConstructor]
        public Serology(string wmdaLocus, string name, SerologySubtype serologySubtype, bool isDeleted = false) 
            : base(wmdaLocus, name, isDeleted)
        {
            SerologySubtype = serologySubtype;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, subtype: {SerologySubtype}";
        }

        public bool Equals(Serology other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && SerologySubtype == other.SerologySubtype;
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
                return (base.GetHashCode() * 397) ^ (int) SerologySubtype;
            }
        }
    }
}
