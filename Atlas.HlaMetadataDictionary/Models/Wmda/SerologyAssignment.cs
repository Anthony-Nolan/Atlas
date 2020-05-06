using System;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda
{
    public class SerologyAssignment : IEquatable<SerologyAssignment>
    {
        public string Name { get; }
        public Assignment Assignment { get; }

        public SerologyAssignment(string name, Assignment assignment)
        {
            Name = name;
            Assignment = assignment;
        }

        public override string ToString()
        {
            return $"{Name} ({Assignment.ToString()})";
        }

        public bool Equals(SerologyAssignment other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Assignment == other.Assignment;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyAssignment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode() * 397) ^ (int) Assignment;
            }
        }
    }
}
