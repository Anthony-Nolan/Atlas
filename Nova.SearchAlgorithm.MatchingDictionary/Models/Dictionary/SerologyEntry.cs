using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    public class SerologyEntry : IEquatable<SerologyEntry>
    {
        public string Name { get; }
        public SerologySubtype SerologySubtype { get; }

        public SerologyEntry(string name, SerologySubtype serologySubtype)
        {
            Name = name;
            SerologySubtype = serologySubtype;
        }

        public bool Equals(SerologyEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && SerologySubtype == other.SerologySubtype;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode() * 397) ^ (int) SerologySubtype;
            }
        }
    }
}
