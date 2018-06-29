using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class AlleleNameHistory : IWmdaHlaTyping, IEquatable<AlleleNameHistory>
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string Locus { get; set; }
        public string Name { get; set; }
        public IEnumerable<VersionedAlleleName> VersionedAlleleNames { get; private set; }
        public string CurrentAlleleName { get; private set; }
        public IEnumerable<string> DistinctAlleleNames { get; private set; }

        public AlleleNameHistory(string locus, string hlaId, IEnumerable<VersionedAlleleName> versionedAlleleNames)
        {
            Locus = locus;
            Name = hlaId;
            SetAlleleNameProperties(versionedAlleleNames);
        }

        private void SetAlleleNameProperties(IEnumerable<VersionedAlleleName> versionedAlleleNames)
        {
            var alleleNamesQuery = versionedAlleleNames.AsQueryable();

            VersionedAlleleNames = alleleNamesQuery;

            CurrentAlleleName = alleleNamesQuery.FirstOrDefault()?.AlleleName;

            DistinctAlleleNames = alleleNamesQuery
                .Select(allele => allele.AlleleName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct();
        }

        public override string ToString()
        {
            return $"locus: {Locus}, hlaId: {Name}";
        }

        public bool Equals(AlleleNameHistory other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                string.Equals(Locus, other.Locus) &&
                string.Equals(Name, other.Name) &&
                VersionedAlleleNames.SequenceEqual(other.VersionedAlleleNames);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlleleNameHistory)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Locus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ VersionedAlleleNames.GetHashCode();
                return hashCode;
            }
        }
    }
}
