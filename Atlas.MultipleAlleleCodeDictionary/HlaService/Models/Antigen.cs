using System;
using Atlas.Common.GeneticData;

namespace Atlas.MultipleAlleleCodeDictionary.HlaService.Models
{
    public class Antigen : IEquatable<Antigen>
    {
        public Locus Locus { get; set; }
        public string HlaName { get; set; }
        public string NmdpString { get; set; }

        #region IEquatable
        public bool Equals(Antigen other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Locus == other.Locus && HlaName == other.HlaName && NmdpString == other.NmdpString;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Antigen) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) Locus, HlaName, NmdpString);
        }

        public static bool operator ==(Antigen left, Antigen right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Antigen left, Antigen right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}