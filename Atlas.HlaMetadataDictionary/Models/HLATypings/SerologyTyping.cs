using Newtonsoft.Json;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using System;

namespace Atlas.HlaMetadataDictionary.Models.HLATypings
{
    public class SerologyTyping : HlaTyping, IEquatable<SerologyTyping>
    {
        public SerologySubtype SerologySubtype { get; }

        public SerologyTyping(SerologyTyping serologyTyping) 
            : this(serologyTyping.TypingLocus, serologyTyping.Name, serologyTyping.SerologySubtype, serologyTyping.IsDeleted)
        {
        }

        public SerologyTyping(IWmdaHlaTyping hlaTyping, SerologySubtype serologySubtype, bool isDeleted = false) 
            : this(hlaTyping.TypingLocus, hlaTyping.Name, serologySubtype, isDeleted)
        {
        }

        [JsonConstructor]
        public SerologyTyping(string typingLocus, string name, SerologySubtype serologySubtype, bool isDeleted = false) 
            : base(TypingMethod.Serology, typingLocus, name, isDeleted)
        {
            SerologySubtype = serologySubtype;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, subtype: {SerologySubtype}";
        }

        public bool Equals(SerologyTyping other)
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
            return Equals((SerologyTyping) obj);
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
