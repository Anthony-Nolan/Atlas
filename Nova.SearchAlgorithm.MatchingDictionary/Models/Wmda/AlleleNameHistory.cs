using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class AlleleNameHistory : IWmdaHlaTyping
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string Locus { get; set; }

        /// <summary>
        /// HLA Accession ID
        /// </summary>
        public string Name { get; set; }
        public IEnumerable<VersionedAlleleName> VersionedAlleleNames { get; private set; }
        public string CurrentAlleleName { get; private set; }
        public IEnumerable<string> DistinctAlleleNames { get; private set; }

        /// <summary>
        /// This property is useful for deleted alleles that have been shown to be identical to another allele.
        /// They will have no current name, but will have had a name assigned to them in the past.
        /// For valid or renamed alleles, values for "current" and "most recent" names will be identical.
        /// </summary>
        public string MostRecentAlleleName { get; private set; }

        public AlleleNameHistory(string locus, string hlaId, IEnumerable<VersionedAlleleName> versionedAlleleNames)
        {
            Locus = locus;
            Name = hlaId;
            SetAlleleNameProperties(versionedAlleleNames);
        }

        private void SetAlleleNameProperties(IEnumerable<VersionedAlleleName> versionedAlleleNames)
        {
            var alleleNamesQuery = versionedAlleleNames
                .OrderByDescending(x => x.HlaDatabaseVersion)
                .ToList();

            VersionedAlleleNames = alleleNamesQuery;

            CurrentAlleleName = alleleNamesQuery
                .FirstOrDefault()
                ?.AlleleName;

            DistinctAlleleNames = alleleNamesQuery
                .Select(allele => allele.AlleleName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct();

            MostRecentAlleleName = DistinctAlleleNames
                .FirstOrDefault();
        }

        public bool DistinctAlleleNamesContain(IWmdaHlaTyping allele)
        {
            return Locus.Equals(allele.Locus) && DistinctAlleleNames.Contains(allele.Name);
        }

        public override string ToString()
        {
            return $"locus: {Locus}, hlaId: {Name}";
        }
    }
}
