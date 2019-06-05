using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class AlleleNameHistory : IWmdaHlaTyping
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TypingLocus { get; set; }

        /// <summary>
        /// HLA Accession ID
        /// </summary>
        public string Name { get; set; }
        public IEnumerable<VersionedAlleleName> VersionedAlleleNames { get; }

        public string CurrentAlleleName => VersionedAlleleNames
            .FirstOrDefault()
            ?.AlleleName;

        public IEnumerable<string> DistinctAlleleNames => VersionedAlleleNames
            .Select(allele => allele.AlleleName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct();

        /// <summary>
        /// This property is useful for deleted alleles that have been shown to be identical to another allele.
        /// They will have no current name, but will have had a name assigned to them in the past.
        /// For valid or renamed alleles, values for "current" and "most recent" names will be identical.
        /// </summary>
        public string MostRecentAlleleName => DistinctAlleleNames
            .FirstOrDefault();

        public AlleleNameHistory(string locus, string hlaId, IEnumerable<VersionedAlleleName> versionedAlleleNames)
        {
            TypingLocus = locus;
            Name = hlaId;
            VersionedAlleleNames = versionedAlleleNames;
        }

        public bool DistinctAlleleNamesContain(IWmdaHlaTyping allele)
        {
            return TypingLocus.Equals(allele.TypingLocus) && DistinctAlleleNames.Contains(allele.Name);
        }

        public override string ToString()
        {
            return $"locus: {TypingLocus}, hlaId: {Name}";
        }
    }
}
